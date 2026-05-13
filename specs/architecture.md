# Zenatur Legacy Bridge — Especificação Arquitetural

> **Projeto separado:** `D:\devops.zenatur\zt-lagacy-bridge`
> **Tipo:** Web API (.NET 8 Minimal APIs) + BackgroundService de polling — **mesmo processo**.
> **Papel:** Anti-Corruption Layer (ACL) entre o novo ecossistema (CIOT API + TMS) e o **SQL Server legado** da Zenatur.
> **Status:** Spec consolidada após brainstorm + decisões. Pronta para implementação.

---

## 1. Visão Geral e Missão

`Zenatur.LegacyBridge` é o único componente do novo ecossistema autorizado a **ler ou escrever** no banco legado. Toda outra peça (CIOT API, TMS) trata o legado como uma **HTTP API REST** isolada — sem JOINs, sem connection string, sem schema legado vazando.

### 1.1 Duas responsabilidades, um único processo

| Faceta | Direção | Consumidor | Propósito |
|--------|---------|------------|-----------|
| **Query API** | TMS → Bridge | TMS Portal (CIOT Stepper Fase 1) | Autopreencher dados ao digitar CPF/placa. |
| **Sync Worker** | Bridge → CIOT API | CIOT API (`/outbox/pending`) | Replicar eventos do novo p/ legado. |

### 1.2 Por que ACL e não EF compartilhado?

- Schema legado é **frágil e proprietário** (`NM_MOT`, `NR_CGC`, `DT_NASC`…). Vazaria ubiquitous language Zenatur.
- Permissões de banco isoladas — só Bridge tem credencial de escrita no legado.
- Disponibilidade desacoplada: legado lento ≠ emissão CIOT lenta.
- Substituível: quando legado for desligado, derruba só o Bridge.

---

## 2. Stack & Padrões

| Item | Escolha | Justificativa |
|------|---------|---------------|
| Runtime | .NET 8 LTS, C# 12 | Mesma do CIOT-API. |
| Tipo | ASP.NET Core Web API (Minimal APIs) + `BackgroundService` | Hospeda HTTP e worker no mesmo binário. |
| Acesso DB legado | **Dapper** (obrigatório) — SQL puro com `WITH (NOLOCK)` em SELECTs | Performance, controle fino, sem migrations no schema legado. |
| Result | `FluentResults` | Regra de Ouro #1. |
| Validação | `FluentValidation` em payloads de sync | Fail-fast. |
| HTTP client (→ CIOT) | `HttpClientFactory` + Polly | Retry/circuit-breaker. |
| Resiliência DB | Polly retry transient (SqlException numbers 1205, 4060, 40197, 40501, 40613, 49918–49920) | Padrão para legacy SQL. |
| Logs | Serilog JSON + rolling file | Estruturado. |
| Health | `AspNetCore.HealthChecks` | `/health/live`, `/health/ready`. |
| Testes | xUnit + Moq + FluentAssertions + Testcontainers (SQL) + WireMock | Espelha CIOT-API. |
| Hospedagem | **Windows Service** (`UseWindowsService()`) | Decisão (d). Servidor Windows on-prem. |

**Proibido:** EF Core no Bridge. Lock-in com schema legado já frágil.

---

## 3. Arquitetura (Diagrama Lógico)

```
                                              GET /v1/legacy/motoristas/{cpf}
                                              GET /v1/legacy/veiculos/{placa}
                                             ◄─────────────────────────────┐
┌────────────────────┐                                                     │
│   TMS Portal       │                                                     │
│   (Blazor SSR)     │                                                     │
└────────┬───────────┘                                                     │
         │ ICiotClient (SDK)                                               │
         ▼                                                                 │
┌────────────────────┐    GET /api/v1/outbox/pending   ┌──────────────────┴──────┐
│  ZT-CIOT-API       │ ◄──────────────────────────────│                          │
│  (ZT_TMS_CIOT)     │                                │   ZT-LEGACY-BRIDGE       │
│                    │ ──── batch JSON ───────────────►│   (Win Service)          │
│  Queue.OutboxMsgs  │                                │   ├ Minimal API endpoints│
│                    │ ◄──── POST /outbox/ack ────────│   ├ OutboxPollingWorker  │
└────────────────────┘                                │   ├ Dapper repos         │
                                                      │   └ X-Api-Key middleware │
                                                      └──────────┬───────────────┘
                                                                 │ Dapper + NOLOCK
                                                                 ▼
                                                      ┌────────────────────────┐
                                                      │   SQL Server Legado    │
                                                      │   (read + write)       │
                                                      └────────────────────────┘
```

---

## 4. Decisões Resolvidas

| ID | Decisão | Implicação |
|----|---------|------------|
| **a** | **Dapper** (não EF Core) | Repositórios Dapper. `WITH (NOLOCK)` em todo SELECT. SQL versionado em arquivos `.sql` embed ou strings. |
| **b** | **`X-Api-Key`** header (shared secret) | Middleware valida; chave em `LegacyBridge__ApiKey` (env var). Bridge **e** CIOT API exigem o mesmo header nos endpoints internos. |
| **c** | **Dedupe via tabela própria** `dbo.LEGACY_BRIDGE_PROCESSED (OutboxId BIGINT PK, MessageType NVARCHAR(100), ProcessedAt DATETIME2)` no banco legado | Centraliza dedupe num único ponto. Independe de chave natural (que varia por `MessageType`). Schema mínimo: 1 tabela, 3 colunas, PK em `OutboxId`. Bridge é o **único writer**. |
| **d** | **Windows Service** | `Microsoft.Extensions.Hosting.WindowsServices` + `UseWindowsService()` no host. Install via `sc.exe` ou script PowerShell. |
| **e** | **`schemaVersion` no payload** | Todo evento JSON do outbox carrega `"schemaVersion": 1`. Handler rejeita versão desconhecida → vai pra DLQ até deploy. |

### 4.1 Detalhe da estratégia de dedupe (c)

```sql
-- Executado UMA VEZ na implantação no banco legado:
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LEGACY_BRIDGE_PROCESSED')
BEGIN
    CREATE TABLE dbo.LEGACY_BRIDGE_PROCESSED (
        OutboxId    BIGINT       NOT NULL PRIMARY KEY,
        MessageType NVARCHAR(100) NOT NULL,
        ProcessedAt DATETIME2     NOT NULL CONSTRAINT DF_LBP_ProcessedAt DEFAULT SYSUTCDATETIME()
    );
END
```

**Fluxo no handler (Dapper, mesma transação):**

```csharp
using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

// 1. Idempotência: insere marcador. Violação de PK = mensagem já processada.
try {
    await conn.ExecuteAsync(
        "INSERT INTO dbo.LEGACY_BRIDGE_PROCESSED (OutboxId, MessageType) VALUES (@id, @type)",
        new { id = msg.Id, type = msg.MessageType }, tx);
} catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) {
    tx.Rollback();
    return Result.Ok(); // já processada → ACK success
}

// 2. Writes de negócio
await _legacyRepo.UpsertCiotEmitidoAsync(payload, tx);

tx.Commit();
return Result.Ok();
```

> **Por que aceitei (c) ao invés de chave natural:** cada `MessageType` futuro (StatusParcela, CancelamentoCiot, etc.) teria sua própria chave natural. Tabela única evita N estratégias de idempotência espalhadas.

---

## 5. Endpoints da Query API (consumo TMS)

> **Auth obrigatória:** `X-Api-Key` em **todos** os endpoints abaixo. Sem header → `401 Unauthorized`.

### 5.1 `GET /v1/legacy/motoristas/{cpf}`

Busca motorista por CPF no legado.

- **Path param:** `cpf` (11 dígitos, sem máscara).
- **Sanitização:** Bridge remove qualquer caractere não-numérico antes da query.
- **Query SQL:** `SELECT ... FROM dbo.MOTORISTAS WITH (NOLOCK) WHERE NR_CPF = @cpf`.

**Response 200:**

```json
{
  "cpf": "12345678901",
  "nome": "JOÃO DA SILVA",
  "dataNascimento": "1980-03-15",
  "telefone": "11999998888",
  "endereco": {
    "logradouro": "RUA X",
    "numero": "100",
    "complemento": null,
    "bairro": "CENTRO",
    "cidadeIbge": "3550308",
    "uf": "SP",
    "cep": "01310100"
  },
  "rntrc": { "numero": "12345678", "ativo": true, "validade": "2027-01-31" }
}
```

**Response 404:** `{ "error": "Motorista não encontrado", "cpf": "..." }`

### 5.2 `GET /v1/legacy/veiculos/{placa}`

Busca veículo por placa.

- **Path param:** `placa` (formato Mercosul ou antigo, maiúsculo, sem hífen).

**Response 200:**

```json
{
  "placa": "ABC1D23",
  "tipoVeiculo": 1,
  "renavam": "00123456789",
  "anoFabricacao": 2018,
  "anoModelo": 2019,
  "marca": "VOLVO",
  "modelo": "FH 540",
  "tara": 8500,
  "capacidadeKg": 25000,
  "proprietario": {
    "documento": "12345678000199",
    "tipoDocumento": "CNPJ",
    "nome": "TRANSPORTES X LTDA"
  }
}
```

### 5.3 Endpoints futuros (placeholder)

- `GET /v1/legacy/favorecidos/{documento}` — favorecido completo (motorista OU PJ) + contas.
- `GET /v1/legacy/proprietarios/{documento}` — proprietário de veículo.
 
---

## 6. Endpoints da Sync API (internos — não-HTTP)

> **Decisão:** os endpoints `POST /v1/legacy/sync/*` do brainstorm **NÃO serão expostos como HTTP**. Razão: o Worker e o Repository vivem no mesmo processo. HTTP entre eles é overhead sem ganho. O Worker chama `ILegacyCiotRepository` diretamente via DI.
>
> Se no futuro o Worker for extraído pra outro deploy, esses endpoints podem ser reintroduzidos sem mudança de contrato.

---

## 7. Outbox Polling Worker

### 7.1 Loop

```
loop forever (cancellation token):
  msgs = ciotApi.GetPendingAsync(size: BatchSize)
  if msgs.IsFailed:
     log warn; sleep(BackoffMs); continue
  if msgs.Value.Count == 0:
     sleep(PollIntervalIdleMs)        # default 5000 ms
     continue

  for msg in msgs.Value:
      result = dispatcher.DispatchAsync(msg)
      ack    = { id: msg.Id, success: result.IsSuccess, errorLog: Stringify(result.Errors) }
      ciotApi.AckAsync(ack)            # se falhar: dedupe table garante idempotência no próximo ciclo

  sleep(PollIntervalBusyMs)            # default 500 ms
```

### 7.2 Dispatcher

```csharp
public interface IOutboxMessageHandler {
    string MessageType { get; }                                 // ex: "Ciot.Emitido"
    int SupportedSchemaVersion { get; }                         // ex: 1
    Task<Result> HandleAsync(OutboxMessageDto msg, CancellationToken ct);
}

public class OutboxDispatcher : IOutboxDispatcher {
    private readonly IReadOnlyDictionary<string, IOutboxMessageHandler> _handlers;
    public Task<Result> DispatchAsync(OutboxMessageDto msg, CancellationToken ct) {
        if (!_handlers.TryGetValue(msg.MessageType, out var h))
            return Task.FromResult(Result.Fail($"No handler for MessageType '{msg.MessageType}'"));
        var version = JsonDocument.Parse(msg.Payload).RootElement
            .GetProperty("schemaVersion").GetInt32();
        if (version != h.SupportedSchemaVersion)
            return Task.FromResult(Result.Fail(
                $"Handler {h.GetType().Name} supports schema v{h.SupportedSchemaVersion}, got v{version}"));
        return h.HandleAsync(msg, ct);
    }
}
```

### 7.3 Retry & DLQ (mesmo do outbox-sql.md)

- ACK `success: false` → API CIOT incrementa `RetryCount`.
- Limite **5 tentativas** → mensagem vira `Status = Failed` no `Queue.OutboxMessages`.
- Replay manual: `UPDATE Queue.OutboxMessages SET Status = 0, RetryCount = 0, ErrorLog = NULL WHERE Id = ?`.

---

## 8. Contratos com a API CIOT

> **Self-contained:** contratos embutidos abaixo. Bridge **não** precisa ler `specs/outbox-sql.md` do repo CIOT.

### 8.0 Tabela `Queue.OutboxMessages` (referência — gerenciada pela CIOT API)

| Coluna | Tipo | Nullable | Descrição |
|--------|------|----------|-----------|
| `Id` | `bigint IDENTITY` | NOT NULL | PK |
| `MessageType` | `nvarchar(100)` | NOT NULL | Ex: `Ciot.Emitido` |
| `Payload` | `nvarchar(max)` | NOT NULL | JSON da mensagem |
| `CreatedAt` | `datetime2` | NOT NULL | Criação |
| `ProcessedAt` | `datetime2` | NULL | Set após ACK success |
| `Status` | `int` | NOT NULL | 0=Pending, 1=Processed, 2=Failed |
| `RetryCount` | `int` DEFAULT 0 | NOT NULL | Incrementado em ACK fail |
| `ErrorLog` | `nvarchar(max)` | NULL | Última falha |

### 8.1 `GET /api/v1/outbox/pending`

Headers obrigatórios: `X-Api-Key: <chave>`.

**Query params:**

| Param | Default | Máximo | Descrição |
|-------|---------|--------|-----------|
| `size` | 50 | 500 | Batch size |

**Response 200** — array (vazio se nenhuma pendente), ordem `CreatedAt` ASC:

```json
[
  {
    "id": 1,
    "messageType": "Ciot.Emitido",
    "payload": "{ ... JSON serializado ... }",
    "createdAt": "2026-05-12T19:00:00Z",
    "retryCount": 0
  }
]
```

**Garantias:**
- GET **não altera** status. Mensagem permanece `Pending` até ACK explícito.
- Single-worker assumption. Sem lock, sem state `Processing`.
- Polling seguro: idempotência delegada ao dedupe table do Bridge.

### 8.2 `POST /api/v1/outbox/ack`

Headers obrigatórios: `X-Api-Key: <chave>`.

**Body:**

```json
{
  "id": 1,
  "success": true,
  "errorLog": null
}
```

**Response 200** — ack registrado (idempotente — ACK em `Processed` retorna 200 sem efeito colateral).
**Response 404** — `id` não existe.

**Comportamento server-side (informativo):**
- `success: true` → `Status = Processed`, `ProcessedAt = UtcNow`.
- `success: false` → `RetryCount++`. Se `RetryCount >= 5` → `Status = Failed`, salva `ErrorLog`. Senão, permanece `Pending` com `ErrorLog` atualizado.

### 8.3 `OutboxController` da API CIOT — atualização necessária

> **TODO CIOT-API (X1 em tasks.md):** controller hoje sem auth. Adicionar middleware/filter exigindo `X-Api-Key` nos dois endpoints. Chave compartilhada via `LegacyBridge:ApiKey` em ambos `appsettings`.

### 8.4 MessageTypes catalogados v1

| MessageType | Origem | schemaVersion |
|-------------|--------|---------------|
| `Ciot.Emitido` | `InsertFreightContractCommandHandler` após D2 Pamcard sucesso | 1 |

Tipos futuros (não escopo v1): `Ciot.Cancelado`, `Parcela.StatusAlterado`, `Pedagio.StatusAlterado`.

---

## 9. Payload `Ciot.Emitido` v1 — CONTRATO IMUTÁVEL

> **Status:** **CONGELADO**. Qualquer mudança = novo `schemaVersion: 2`, novo handler.
> Bridge **deve** rejeitar payload se `schemaVersion != 1` (DLQ após retry).

### 9.1 Exemplo completo

```json
{
  "schemaVersion": 1,
  "protocoloCiot": "CIOT202605120001",
  "emitidoEm": "2026-05-12T14:30:00Z",
  "snapshotAuditoriaId": "8f7e6d5c-4b3a-2918-7654-3210fedcba98",
  "contratante": {
    "documento": "12345678000199",
    "razaoSocial": "ZENATUR LTDA"
  },
  "favorecido": {
    "documento": "12345678901",
    "tipoDocumento": "CPF",
    "nome": "JOAO DA SILVA",
    "rntrc": "12345678"
  },
  "veiculo": {
    "placa": "ABC1D23",
    "tipo": 1
  },
  "rota": {
    "origemIbge": "3550308",
    "destinoIbge": "3304557",
    "paradas": ["3509502"]
  },
  "valores": {
    "valorBruto": 10000.00,
    "valorPedagio": 200.00,
    "valorImpostos": 350.00,
    "moeda": "BRL"
  },
  "parcelas": [
    { "numero": 1, "valor": 5000.00, "vencimento": "2026-06-01", "meioPagamento": 28 },
    { "numero": 2, "valor": 5000.00, "vencimento": "2026-07-01", "meioPagamento": 28 }
  ]
}
```

### 9.2 Schema de campos

| Caminho | Tipo | Obrigatório | Regra |
|---------|------|-------------|-------|
| `schemaVersion` | int | sim | **Deve ser `1`**. Outros valores → DLQ. |
| `protocoloCiot` | string | sim | Protocolo retornado pela API CIOT/SEFAZ. |
| `emitidoEm` | string ISO 8601 UTC | sim | Convertido p/ `E. South America Standard Time` ao gravar legado. |
| `snapshotAuditoriaId` | GUID | sim | Referência cruzada com `Audit.Snapshots` na CIOT API. |
| `contratante.documento` | string | sim | CNPJ digits only (14 chars). |
| `contratante.razaoSocial` | string | sim | Max 200 chars. |
| `favorecido.documento` | string | sim | CPF (11) ou CNPJ (14) digits only. |
| `favorecido.tipoDocumento` | enum | sim | `"CPF"` ou `"CNPJ"`. |
| `favorecido.nome` | string | sim | Max 200 chars. |
| `favorecido.rntrc` | string | não | 8 dígitos. Pode ser null para CNPJ-frotista. |
| `veiculo.placa` | string | sim | Mercosul (`ABC1D23`) ou antigo (`ABC1234`). Uppercase, sem hífen. |
| `veiculo.tipo` | int | sim | Código tipo veículo SEFAZ. |
| `rota.origemIbge` | string | sim | Código IBGE 7 dígitos. |
| `rota.destinoIbge` | string | sim | Código IBGE 7 dígitos. |
| `rota.paradas` | string[] | sim | Array (pode ser vazio) de códigos IBGE intermediários. |
| `valores.valorBruto` | decimal | sim | `decimal(18,2)`, ≥ 0. |
| `valores.valorPedagio` | decimal | sim | `decimal(18,2)`, ≥ 0. |
| `valores.valorImpostos` | decimal | sim | `decimal(18,2)`, ≥ 0. |
| `valores.moeda` | string | sim | `"BRL"` v1. |
| `parcelas` | array | sim | Min 1 item. |
| `parcelas[].numero` | int | sim | Sequencial 1..N. |
| `parcelas[].valor` | decimal | sim | `decimal(18,2)`, > 0. Soma = `valorBruto` (tolerância 0.01). |
| `parcelas[].vencimento` | string (date) | sim | ISO 8601 `YYYY-MM-DD`. |
| `parcelas[].meioPagamento` | int | sim | Código SEFAZ (ex 28=PIX). |

### 9.3 Regras de processamento

- Datas UTC ISO 8601. Bridge converte para horário local `E. South America Standard Time` se schema legado guardar local.
- Documentos sem máscara (digits only). FluentValidation rejeita máscara/letras.
- Decimais com ponto, **nunca** vírgula. `decimal(18,2)` no Dapper (`DbType.Decimal`, precision 18, scale 2).
- Validação completa via `CiotEmitidoPayloadValidator` (FluentValidation) **antes** de qualquer write SQL.

---

## 10. Mapeamento → Base Legada (Ciot.Emitido)

> **TODO bloqueante:** inventário real das tabelas. Tabela abaixo é gabarito.

| Campo Payload | Tabela Legada | Coluna | Transform |
|---------------|---------------|--------|-----------|
| `protocoloCiot` | `dbo.CIOT_EMISSAO` | `NR_PROTOCOLO` | direto |
| `emitidoEm` | `dbo.CIOT_EMISSAO` | `DT_EMISSAO` | UTC → America/Sao_Paulo |
| `contratante.documento` | `dbo.CIOT_EMISSAO` | `NR_CGC_CONTRATANTE` | digits only |
| `favorecido.documento` | `dbo.CIOT_EMISSAO` | `NR_DOC_FAV` | digits only |
| `veiculo.placa` | `dbo.CIOT_EMISSAO` | `CD_PLACA` | uppercase, sem hífen |
| `rota.origemIbge` | `dbo.CIOT_EMISSAO` | `CD_IBGE_ORIG` | direto |
| `valores.valorBruto` | `dbo.CIOT_EMISSAO` | `VL_BRUTO` | `decimal(18,2)` |
| `parcelas[]` | `dbo.CIOT_PARCELAS` | `NR_PARCELA`, `VL_PARC`, `DT_VENC` | 1 row por parcela |
| `snapshotAuditoriaId` | `dbo.CIOT_EMISSAO` | `ID_SNAPSHOT_AUD` | uniqueidentifier |

**Comando SQL exemplo (Dapper, dentro da transação de dedupe):**

```sql
-- UPSERT defensivo. Se PK violou, dedupe já abortou no INSERT da LEGACY_BRIDGE_PROCESSED.
INSERT INTO dbo.CIOT_EMISSAO
    (NR_PROTOCOLO, DT_EMISSAO, NR_CGC_CONTRATANTE, NR_DOC_FAV, CD_PLACA,
     CD_IBGE_ORIG, CD_IBGE_DEST, VL_BRUTO, VL_PEDAGIO, VL_IMPOSTOS,
     ID_SNAPSHOT_AUD, DT_INSERT)
VALUES
    (@protocoloCiot, @emitidoEmLocal, @cnpjContratante, @docFav, @placa,
     @ibgeOrig, @ibgeDest, @valorBruto, @valorPedagio, @valorImpostos,
     @snapshotAudId, SYSDATETIME());
```

---

## 10-bis. DDL Gabarito Tabelas Legadas (Testcontainers seed)

> **Uso:** schema **fictício** para Testcontainers + dev local enquanto inventário real (B1) não chega. Quando schema real for confirmado, substituir nestes seeds. Colunas e tipos refletem a tabela §10 — qualquer divergência com prod será resolvida via novos DDLs.

```sql
-- ============================================
-- MOTORISTAS (consulta GET /motoristas/{cpf})
-- ============================================
CREATE TABLE dbo.MOTORISTAS (
    NR_CPF        VARCHAR(11)   NOT NULL PRIMARY KEY,
    NM_MOT        NVARCHAR(200) NOT NULL,
    DT_NASC       DATE          NULL,
    NR_TELEFONE   VARCHAR(20)   NULL,
    DS_LOGRADOURO NVARCHAR(200) NULL,
    NR_ENDERECO   VARCHAR(20)   NULL,
    DS_COMPL      NVARCHAR(100) NULL,
    DS_BAIRRO     NVARCHAR(100) NULL,
    CD_IBGE       VARCHAR(7)    NULL,
    SG_UF         CHAR(2)       NULL,
    NR_CEP        VARCHAR(8)    NULL,
    NR_RNTRC      VARCHAR(8)    NULL,
    FL_RNTRC_ATIVO BIT          NULL,
    DT_RNTRC_VAL  DATE          NULL
);
CREATE INDEX IX_MOTORISTAS_RNTRC ON dbo.MOTORISTAS(NR_RNTRC);

-- ============================================
-- VEICULOS (consulta GET /veiculos/{placa})
-- ============================================
CREATE TABLE dbo.VEICULOS (
    CD_PLACA      VARCHAR(7)    NOT NULL PRIMARY KEY,
    CD_TIPO_VEIC  INT           NOT NULL,
    NR_RENAVAM    VARCHAR(11)   NULL,
    NR_ANO_FAB    INT           NULL,
    NR_ANO_MOD    INT           NULL,
    NM_MARCA      NVARCHAR(50)  NULL,
    NM_MODELO     NVARCHAR(100) NULL,
    QT_TARA       INT           NULL,
    QT_CAP_KG     INT           NULL,
    NR_DOC_PROP   VARCHAR(14)   NULL,
    TP_DOC_PROP   CHAR(4)       NULL,    -- 'CPF ' ou 'CNPJ'
    NM_PROP       NVARCHAR(200) NULL
);
CREATE INDEX IX_VEICULOS_PROP ON dbo.VEICULOS(NR_DOC_PROP);

-- ============================================
-- CIOT_EMISSAO (write via OutboxHandler)
-- ============================================
CREATE TABLE dbo.CIOT_EMISSAO (
    NR_PROTOCOLO         VARCHAR(50)    NOT NULL PRIMARY KEY,
    DT_EMISSAO           DATETIME2      NOT NULL,
    NR_CGC_CONTRATANTE   VARCHAR(14)    NOT NULL,
    NM_CONTRATANTE       NVARCHAR(200)  NOT NULL,
    NR_DOC_FAV           VARCHAR(14)    NOT NULL,
    TP_DOC_FAV           CHAR(4)        NOT NULL,    -- 'CPF ' ou 'CNPJ'
    NM_FAV               NVARCHAR(200)  NOT NULL,
    NR_RNTRC_FAV         VARCHAR(8)     NULL,
    CD_PLACA             VARCHAR(7)     NOT NULL,
    CD_TIPO_VEIC         INT            NOT NULL,
    CD_IBGE_ORIG         VARCHAR(7)     NOT NULL,
    CD_IBGE_DEST         VARCHAR(7)     NOT NULL,
    DS_PARADAS           NVARCHAR(500)  NULL,         -- CSV de códigos IBGE
    VL_BRUTO             DECIMAL(18,2)  NOT NULL,
    VL_PEDAGIO           DECIMAL(18,2)  NOT NULL,
    VL_IMPOSTOS          DECIMAL(18,2)  NOT NULL,
    CD_MOEDA             CHAR(3)        NOT NULL DEFAULT 'BRL',
    ID_SNAPSHOT_AUD      UNIQUEIDENTIFIER NOT NULL,
    DT_INSERT            DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);

-- ============================================
-- CIOT_PARCELAS (write — 1 row por parcela)
-- ============================================
CREATE TABLE dbo.CIOT_PARCELAS (
    NR_PROTOCOLO   VARCHAR(50)   NOT NULL,
    NR_PARCELA     INT           NOT NULL,
    VL_PARC        DECIMAL(18,2) NOT NULL,
    DT_VENC        DATE          NOT NULL,
    CD_MEIO_PAG    INT           NOT NULL,
    DT_INSERT      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_CIOT_PARCELAS PRIMARY KEY (NR_PROTOCOLO, NR_PARCELA),
    CONSTRAINT FK_CIOT_PARCELAS_EMISSAO
        FOREIGN KEY (NR_PROTOCOLO) REFERENCES dbo.CIOT_EMISSAO(NR_PROTOCOLO)
);

-- ============================================
-- LEGACY_BRIDGE_PROCESSED (dedupe — Bridge único writer)
-- ============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LEGACY_BRIDGE_PROCESSED')
BEGIN
    CREATE TABLE dbo.LEGACY_BRIDGE_PROCESSED (
        OutboxId    BIGINT        NOT NULL PRIMARY KEY,
        MessageType NVARCHAR(100) NOT NULL,
        ProcessedAt DATETIME2     NOT NULL CONSTRAINT DF_LBP_ProcessedAt DEFAULT SYSUTCDATETIME()
    );
END
```

### Seed de teste mínimo

```sql
INSERT INTO dbo.MOTORISTAS (NR_CPF, NM_MOT, DT_NASC, NR_TELEFONE, DS_LOGRADOURO, NR_ENDERECO,
                            DS_BAIRRO, CD_IBGE, SG_UF, NR_CEP, NR_RNTRC, FL_RNTRC_ATIVO, DT_RNTRC_VAL)
VALUES ('12345678901', 'JOAO DA SILVA', '1980-03-15', '11999998888', 'RUA X', '100',
        'CENTRO', '3550308', 'SP', '01310100', '12345678', 1, '2027-01-31');

INSERT INTO dbo.VEICULOS (CD_PLACA, CD_TIPO_VEIC, NR_RENAVAM, NR_ANO_FAB, NR_ANO_MOD,
                          NM_MARCA, NM_MODELO, QT_TARA, QT_CAP_KG, NR_DOC_PROP, TP_DOC_PROP, NM_PROP)
VALUES ('ABC1D23', 1, '00123456789', 2018, 2019,
        'VOLVO', 'FH 540', 8500, 25000, '12345678000199', 'CNPJ', 'TRANSPORTES X LTDA');
```

---

## 11. Estrutura de Pastas

```
zt-lagacy-bridge/
├── Zenatur.LegacyBridge.sln
├── src/
│   ├── Zenatur.LegacyBridge/                     # Web API + Worker host
│   │   ├── Program.cs                            # WebApplicationBuilder + UseWindowsService + Serilog + DI
│   │   ├── Endpoints/
│   │   │   ├── MotoristasEndpoints.cs            # MapGroup("/v1/legacy/motoristas")
│   │   │   ├── VeiculosEndpoints.cs
│   │   │   └── HealthEndpoints.cs
│   │   ├── Middleware/
│   │   │   └── ApiKeyMiddleware.cs               # valida X-Api-Key
│   │   ├── Workers/
│   │   │   └── OutboxPollingWorker.cs            # BackgroundService
│   │   ├── HealthChecks/
│   │   │   ├── CiotApiHealthCheck.cs
│   │   │   └── LegacyDbHealthCheck.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   ├── Zenatur.LegacyBridge.Application/
│   │   ├── Ports/
│   │   │   ├── ICiotApiClient.cs
│   │   │   ├── IMotoristaRepository.cs           # query side
│   │   │   ├── IVeiculoRepository.cs
│   │   │   ├── ILegacyCiotRepository.cs          # sync side
│   │   │   ├── IOutboxMessageHandler.cs
│   │   │   └── IOutboxDispatcher.cs
│   │   ├── Queries/
│   │   │   ├── GetMotoristaByCpf/
│   │   │   │   ├── GetMotoristaByCpfQuery.cs
│   │   │   │   ├── GetMotoristaByCpfHandler.cs
│   │   │   │   └── MotoristaDto.cs
│   │   │   └── GetVeiculoByPlaca/...
│   │   ├── OutboxHandlers/
│   │   │   └── CiotEmitido/
│   │   │       ├── CiotEmitidoHandler.cs         # implements IOutboxMessageHandler
│   │   │       ├── CiotEmitidoPayload.cs
│   │   │       ├── CiotEmitidoPayloadValidator.cs
│   │   │       └── CiotEmitidoMapper.cs
│   │   ├── Dispatching/
│   │   │   └── OutboxDispatcher.cs
│   │   └── Common/
│   │       ├── OutboxMessageDto.cs
│   │       └── AckRequest.cs
│   │
│   └── Zenatur.LegacyBridge.Infrastructure/
│       ├── Http/
│       │   ├── CiotApiClient.cs                  # HttpClientFactory + Polly
│       │   ├── CiotApiOptions.cs
│       │   └── ApiKeyDelegatingHandler.cs        # injeta X-Api-Key outbound
│       ├── Persistence/Dapper/
│       │   ├── DapperConnectionFactory.cs        # IDbConnection scoped
│       │   ├── LegacyDbOptions.cs
│       │   ├── MotoristaRepository.cs
│       │   ├── VeiculoRepository.cs
│       │   ├── LegacyCiotRepository.cs
│       │   └── Sql/                              # arquivos .sql embed
│       │       ├── motorista_by_cpf.sql
│       │       ├── veiculo_by_placa.sql
│       │       ├── insert_ciot_emissao.sql
│       │       ├── insert_ciot_parcelas.sql
│       │       └── upsert_dedupe.sql
│       └── DependencyInjection.cs
│
└── tests/
    ├── Zenatur.LegacyBridge.UnitTests/
    │   ├── Queries/...                           # com Moq dos repos
    │   ├── OutboxHandlers/CiotEmitido/...
    │   ├── Dispatching/...
    │   └── Http/...
    └── Zenatur.LegacyBridge.IntegrationTests/
        ├── Endpoints/MotoristasEndpointsTests.cs # WebApplicationFactory + Testcontainers
        └── Workers/OutboxFlowTests.cs            # WireMock CIOT + Testcontainers SQL
```

---

## 12. Configuração

### 12.1 `appsettings.json`

```json
{
  "CiotApi": {
    "BaseUrl": "https://ciot.internal.zenatur.com.br",
    "ApiKey": "set-via-env",
    "BatchSize": 50,
    "PollIntervalIdleMs": 5000,
    "PollIntervalBusyMs": 500,
    "BackoffMsOnFailure": 10000,
    "HttpTimeoutSeconds": 30
  },
  "LegacyDb": {
    "ConnectionString": "set-via-env",
    "CommandTimeoutSeconds": 30
  },
  "InboundApiKey": "set-via-env",
  "Serilog": { "MinimumLevel": "Information" }
}
```

### 12.2 Env vars em produção

| Env var | Sensibilidade |
|---------|---------------|
| `CiotApi__ApiKey` | secret (igual à `LegacyBridge:ApiKey` no `appsettings` da CIOT-API) |
| `LegacyDb__ConnectionString` | secret |
| `InboundApiKey` | secret (chave que TMS envia ao chamar Bridge) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### 12.3 TMS — configuração para chamar Bridge

```json
{
  "LegacyBridge": {
    "BaseUrl": "https://bridge.internal.zenatur.com.br",
    "ApiKey": "set-via-env"
  }
}
```

`Zenatur.Tms.Web` precisa de novo `ILegacyBridgeClient` (SDK fininho) ou injeção direta de `HttpClient` tipado.

> **TODO TMS:** criar `LegacyBridgeClient.cs` no projeto Infrastructure do TMS.

---

## 13. Resiliência

| Cenário | Comportamento |
|---------|---------------|
| API CIOT offline | Polly retry 3x (1s→2s→4s); depois loga `Error`, dorme `BackoffMsOnFailure`. Worker continua vivo. Endpoints HTTP do Bridge **continuam atendendo queries** normalmente. |
| Legacy DB offline | Repos retornam `Result.Fail`; Worker ACK `success: false`; queries HTTP retornam 503 com `Retry-After`. |
| Payload malformado / `schemaVersion` desconhecido | Handler retorna `Result.Fail`; após 5 tentativas vai pra DLQ. |
| ACK falha após write OK | Dedupe via `LEGACY_BRIDGE_PROCESSED` garante no-op no próximo ciclo. |
| Worker crash | Windows Service `RestartOnFailure`. Estado em SQL — recovery é só voltar a polling. |
| Deadlock no legado (Err 1205) | Polly retry transient 3x dentro do handler antes de devolver `Result.Fail`. |

---

## 14. Observabilidade

### 14.1 Logs estruturados (Serilog)

Campos comuns: `outboxId`, `messageType`, `schemaVersion`, `retryCount`, `durationMs`, `cpf` (mascarado: 3 primeiros + `***`), `placa`.

### 14.2 Eventos

| Nível | Evento |
|-------|--------|
| Info | `OutboxFetched`, `MessageProcessed`, `QuerySucceeded` |
| Warn | `MessageHandlerFailed`, `QueryNotFound`, `ApiKeyMissing` (toda chamada sem header) |
| Error | `CiotApiUnreachable`, `LegacyDbUnreachable`, `AckFailed`, `UnknownMessageType`, `SchemaVersionMismatch` |

### 14.3 Health endpoints

`GET http://0.0.0.0:5081/health/live` — processo vivo.
`GET http://0.0.0.0:5081/health/ready` — Legacy DB + CIOT API alcançáveis.

### 14.4 PII

CPF/CNPJ **mascarados** em logs (`123******01`). Logs com PII completo proibidos.

---

## 15. Testes

### 15.1 Unit (xUnit + Moq + FluentAssertions)

- `GetMotoristaByCpfHandler` com `IMotoristaRepository` mock — testa sanitização do CPF + 200/404.
- `CiotEmitidoHandler` com `ILegacyCiotRepository` + `IDbConnection` mock — testa happy path, dedupe-hit, payload validation fail, schema version mismatch.
- `OutboxDispatcher` — tipo desconhecido, versão errada, handler success/fail.
- `CiotApiClient` com `HttpMessageHandler` mock — 200, 401, 500, timeout, retry exhaustion.

### 15.2 Integration (Testcontainers + WireMock)

- Sobe `mcr.microsoft.com/mssql/server:2022-latest` com seed do schema mínimo legado.
- WireMock simula `/outbox/pending` e `/outbox/ack`.
- Cenários: happy path E2E, ACK falha pós-write (verifica dedupe), legacy DB deadlock, mensagem duplicada.
- Endpoint test: `WebApplicationFactory` para validar `X-Api-Key` + 200/404 do `GET /motoristas/{cpf}`.

### 15.3 Cobertura mínima

- `Application/` 90%.
- `Infrastructure/Persistence/Dapper/` 70% (excluindo SQL puro coberto por integration).

---

## 16. Roadmap de Implementação

### Fase 0 — Bloqueantes (precisam de input do time)

- [ ] **Inventário schema legado:** tabelas `MOTORISTAS`, `VEICULOS`, `PROPRIETARIOS`, `CIOT_EMISSAO`, `CIOT_PARCELAS` (nomes reais, colunas, PKs, indices).
- [ ] **Connection string** do legado em DEV.
- [ ] **Chave `X-Api-Key`** acordada entre Bridge + CIOT API + TMS.
- [ ] **Congelamento payload** `Ciot.Emitido` v1 (lado CIOT-API). Adicionar `schemaVersion: 1` na serialização do handler `InsertFreightContractCommandHandler`.
- [ ] **Criação da tabela** `dbo.LEGACY_BRIDGE_PROCESSED` em DEV/HML/PRD legado.

### Fase 1 — Esqueleto

- Solution + projetos + NuGets (Dapper, Polly, Serilog, FluentResults, FluentValidation).
- `Program.cs` com Web API + `BackgroundService` registrados, Serilog, health checks, DI vazio.
- `ApiKeyMiddleware` funcional (rejeita 401 sem header).
- Worker que apenas loga "tick" e bate em `/outbox/pending` (sem processar).

### Fase 2 — Query API

- `IMotoristaRepository` Dapper + SQL `WITH (NOLOCK)`.
- Endpoint `GET /v1/legacy/motoristas/{cpf}` + handler + validator.
- Testes unit + integration.
- `IVeiculoRepository` análogo + endpoint.

### Fase 3 — Outbox Worker (sem dedupe)

- `ICiotApiClient` + Polly.
- `OutboxDispatcher` + `IOutboxMessageHandler` infra.
- `CiotEmitidoHandler` retornando `Result.Ok()` (no-op) — loop end-to-end validado.

### Fase 4 — Persistência sync

- Tabela `LEGACY_BRIDGE_PROCESSED` criada.
- `ILegacyCiotRepository` + SQL real.
- `CiotEmitidoMapper` + `CiotEmitidoPayloadValidator` + `schemaVersion` check.
- Cobertura de teste completa.

### Fase 5 — Operação

- Script PowerShell `install-service.ps1` (`sc.exe create ZenaturLegacyBridge ...`).
- Runbook: replay de Failed, troubleshooting, métricas no log.
- Update na API CIOT: `X-Api-Key` middleware no `OutboxController`.
- Update no TMS: `LegacyBridgeClient` + integração no merge da Fase 1 do Stepper.

---

## 17. Pendências para Outras Specs

Mudanças que esta spec **dispara** em outros documentos:

| Spec | Mudança |
|------|---------|
| `specs/outbox-sql.md` | Adicionar `schemaVersion` no envelope do payload. Adicionar `X-Api-Key` nos endpoints `/outbox/pending` e `/outbox/ack`. |
| `specs/tms/ciot-module.md` §3 Fase 1 | Merge passa de 2 para 3 fontes (CIOT API + Pamcard + **Bridge**). Bridge é prioridade em dados cadastrais. |
| `specs/tms/tasks.md` T11 | Incluir chamada ao Bridge no `Task.WhenAll`. |
| Memory `project_state.md` (TMS) | Atualizar T11 para mencionar Bridge. |

---

## 18. Glossário

| Termo | Significado |
|-------|-------------|
| **Bridge** | Este projeto: `Zenatur.LegacyBridge`. |
| **ACL (Anti-Corruption Layer)** | Camada que isola o novo ecossistema do schema legado. |
| **Outbox** | `Queue.OutboxMessages` em `ZT_TMS_CIOT`. Origem dos eventos sync. |
| **DLQ lógica** | `Status = Failed` no `Queue.OutboxMessages`. Replay manual via SQL. |
| **Dedupe table** | `dbo.LEGACY_BRIDGE_PROCESSED` no banco legado. Bridge é único writer. |
| **schemaVersion** | Inteiro no payload JSON; handler rejeita versão não suportada. |
