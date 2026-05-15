# Outbox Payloads v1 — Exemplos JSON

> Contrato JSON v1 emitido pelo CIOT API (`Queue.OutboxMessages.Payload`) e consumido pelo Bridge.
> Última atualização: 2026-05-14
> Implementação: `Zenatur.Ciot.Application.Outbox.Payloads.V1.*` + `OutboxService.JsonOptions`

## Convenções gerais

- **Naming:** `camelCase` em todas as keys
- **Null fields:** **omitidos** (`DefaultIgnoreCondition = WhenWritingNull`)
- **Datas:** `DateOnly` → `"yyyy-MM-dd"`; `DateTime` → ISO 8601 com `Z` (UTC)
- **Enums:** strings em `camelCase` (ex: `TipoTracao` → `"tracionante"` / `"reboque"`)
- **Strings vazias** ficam (mantém compatibilidade com Bridge `marca: ""`)
- **Campos obrigatórios sempre presentes:** `schemaVersion: 1`, `messageType`, `occurredAt`, `contratanteCnpj`

## Idempotência

Cada mensagem tem um ID outbox (sequencial bigint). Bridge dedupe pela tabela `LEGACY_BRIDGE_PROCESSED` (já existe pra `Ciot.Emitido`) usando esse ID.

---

## Favorecido

### `Favorecido.Criado` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Favorecido.Criado",
  "occurredAt": "2026-05-14T15:00:00Z",
  "contratanteCnpj": "53717120000170",
  "favorecido": {
    "documento": "37420761833",
    "documentoTipo": 2,
    "nome": "JOAO DA SILVA",
    "dataNascimento": "1985-03-15",
    "email": "joao@email.com",
    "rntrc": "12345678",
    "rntrcSituacao": "Ativo",
    "telefoneDdd": "11",
    "telefoneNumero": "987654321",
    "endereco": {
      "logradouro": "RUA DAS FLORES",
      "numero": 123,
      "bairro": "CENTRO",
      "cidade": "São Paulo",
      "uf": "SP",
      "cep": "01000000",
      "cidadeIbge": 3550308
    },
    "ativo": true
  }
}
```

**Origem:** `POST /api/v1/favorecidos` (chama Pamcard `InsertFavored` op 23 primeiro).
**Ação Bridge:** UPSERT na tabela `MOTORISTAS` (ou `FAVORECIDOS` legado) por `(ContratanteCnpj, Documento)`.

### `Favorecido.Atualizado` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Favorecido.Atualizado",
  "occurredAt": "2026-05-14T15:05:00Z",
  "contratanteCnpj": "53717120000170",
  "favorecido": {
    "documento": "37420761833",
    "documentoTipo": 2,
    "nome": "JOAO DA SILVA SANTOS",
    "dataNascimento": "1985-03-15",
    "telefoneDdd": "11",
    "telefoneNumero": "987654321",
    "endereco": {
      "logradouro": "RUA NOVA",
      "numero": 456,
      "bairro": "JARDIM",
      "cidade": "São Paulo",
      "uf": "SP",
      "cep": "02000000"
    },
    "ativo": true
  }
}
```

**Origem:** `PUT /api/v1/favorecidos/{doc}` ou `DELETE /api/v1/favorecidos/{doc}` (com `ativo: false`).
**Ação Bridge:** UPDATE do registro existente. Se `ativo: false` → soft-delete no legado.

### `Favorecido.ContaAdicionada` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Favorecido.ContaAdicionada",
  "occurredAt": "2026-05-14T15:10:00Z",
  "contratanteCnpj": "53717120000170",
  "favorecidoDocumento": "37420761833",
  "favorecidoDocumentoTipo": 2,
  "conta": {
    "id": 42,
    "banco": 237,
    "agencia": "0001",
    "agenciaDigito": "0",
    "numero": "123456",
    "tipo": 1,
    "chavePixTipo": 2,
    "chavePix": "user@email.com",
    "pambankIndicador": "S",
    "status": "Ativa"
  }
}
```

**Origem:** `POST /api/v1/favorecidos/{doc}/contas` (chama Pamcard `InsertFavoredAccount` op 25 primeiro).
**Ação Bridge:** INSERT em `CONTAS_BANCARIAS` legado.

### `Favorecido.ContaAtualizada` v1

Mesmo schema da `ContaAdicionada`, só muda `messageType`. UPSERT por `(favorecidoDocumento, banco, agencia, numero)`.

### `Favorecido.ContaRemovida` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Favorecido.ContaRemovida",
  "occurredAt": "2026-05-14T15:15:00Z",
  "contratanteCnpj": "53717120000170",
  "favorecidoDocumento": "37420761833",
  "contaId": 42
}
```

**Ação Bridge:** Soft-delete (marca inativa) na tabela `CONTAS_BANCARIAS`.

---

## Motorista

### `Motorista.Criado` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Motorista.Criado",
  "occurredAt": "2026-05-14T15:20:00Z",
  "contratanteCnpj": "53717120000170",
  "motorista": {
    "cpf": "12447091826",
    "nome": "GILDASIO SANTANA DA CRUZ",
    "dataNascimento": "1980-07-22",
    "email": "gildasio@email.com",
    "rntrc": "98765432",
    "rntrcSituacao": "Ativo",
    "rntrcValidade": "2027-12-31",
    "telefoneDdd": "11",
    "telefoneNumero": "987654321",
    "celularDdd": "11",
    "celularNumero": "999998888",
    "endereco": {
      "logradouro": "RUA DOS CEDROS",
      "numero": 560,
      "bairro": "JARDIM SAPOPEMBA",
      "uf": "SP",
      "cep": "09973310"
    },
    "cnhNumero": "12345678901",
    "cnhCategoria": "E",
    "cnhValidade": "2030-06-30",
    "ativo": true
  }
}
```

**Origem:** `POST /api/v1/motoristas`.
**Ação Bridge:** UPSERT em `MOTORISTAS_CONDUTORES` legado (nome a confirmar com equipe Zenatur) por `(ContratanteCnpj, Cpf)`.

### `Motorista.Atualizado` v1

Mesmo schema da `Motorista.Criado` com `messageType` diferente. UPSERT.

---

## Vínculo Favorecido ↔ Motorista

### `FavorecidoMotorista.Vinculado` v1

```json
{
  "schemaVersion": 1,
  "messageType": "FavorecidoMotorista.Vinculado",
  "occurredAt": "2026-05-14T15:25:00Z",
  "contratanteCnpj": "53717120000170",
  "favorecidoDocumento": "53717120000170",
  "favorecidoDocumentoTipo": 1,
  "motoristaCpf": "12447091826"
}
```

**Origem:** `POST /api/v1/favorecidos/{doc}/motoristas` body `{ "cpf": "..." }`.
**Ação Bridge:** INSERT em `LEGACY_FAVORECIDO_MOTORISTA_LINK` (nome a confirmar). Idempotente (chave composta).

### `FavorecidoMotorista.Desvinculado` v1

Mesmo schema, `messageType: "FavorecidoMotorista.Desvinculado"`.
**Ação Bridge:** UPDATE `DesvinculadoEm = now()` no link existente.

---

## Veículo

### `Veiculo.Criado` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Veiculo.Criado",
  "occurredAt": "2026-05-14T15:30:00Z",
  "contratanteCnpj": "53717120000170",
  "veiculo": {
    "placa": "HTC1321",
    "renavam": "12312312312",
    "categoriaVeiculo": "4",
    "tipoTracao": "tracionante",
    "eixos": 3,
    "anoFabricacao": 2016,
    "anoModelo": 2016,
    "marca": "VOLVO",
    "modelo": "FH 540",
    "cor": "BRANCA",
    "tara": 11500.00,
    "capacidadeKg": 30000.00,
    "rntrc": "12345678",
    "rntrcSituacao": "Ativo",
    "rntrcValidade": "2027-12-31",
    "proprietarioFavorecidoDocumento": "53717120000170",
    "ativo": true
  }
}
```

**Origem:** `POST /api/v1/veiculos`.
**Ação Bridge:** UPSERT em `VEICULOS` legado por `(ContratanteCnpj, Placa)`.

### `Veiculo.Atualizado` v1

Mesmo schema da `Veiculo.Criado` com `messageType` diferente. UPSERT.

### `Veiculo.Removido` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Veiculo.Removido",
  "occurredAt": "2026-05-14T15:35:00Z",
  "contratanteCnpj": "53717120000170",
  "placa": "HTC1321"
}
```

**Ação Bridge:** UPDATE `Ativo = false` em `VEICULOS` legado.

---

## Composição (cavalo + reboques)

### `Composicao.Criada` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Composicao.Criada",
  "occurredAt": "2026-05-14T15:40:00Z",
  "contratanteCnpj": "53717120000170",
  "composicao": {
    "id": 1,
    "nome": "Bitrem Volvo 2024",
    "tracionantePlaca": "HTC1321",
    "reboques": [
      { "placa": "REB0001", "ordem": 1 },
      { "placa": "REB0002", "ordem": 2 }
    ],
    "favorecidoTitularDocumento": "53717120000170",
    "ativo": true
  }
}
```

**Origem:** `POST /api/v1/composicoes`.
**Ação Bridge:** INSERT em `COMPOSICOES` + INSERT em `COMPOSICOES_REBOQUES` (relacionamento N).

### `Composicao.Atualizada` v1

Mesmo schema. Bridge faz:
1. UPDATE `COMPOSICOES` (nome, titular)
2. DELETE all `COMPOSICOES_REBOQUES` da composição
3. INSERT lista nova de reboques (ordem mantida)

### `Composicao.Removida` v1

```json
{
  "schemaVersion": 1,
  "messageType": "Composicao.Removida",
  "occurredAt": "2026-05-14T15:45:00Z",
  "contratanteCnpj": "53717120000170",
  "composicaoId": 1
}
```

**Ação Bridge:** UPDATE `Ativo = false` em `COMPOSICOES` legado. `COMPOSICOES_REBOQUES` ficam (histórico).

---

## Tabela resumo (15 tipos)

| MessageType                            | Quando emitido                                     | Bridge ação              |
|----------------------------------------|----------------------------------------------------|--------------------------|
| `Favorecido.Criado`                    | POST `/favorecidos` (após Pamcard OK)              | UPSERT FAVORECIDOS       |
| `Favorecido.Atualizado`                | PUT `/favorecidos/{doc}` ou DELETE                 | UPDATE                   |
| `Favorecido.ContaAdicionada`           | POST `/favorecidos/{doc}/contas`                   | INSERT CONTAS_BANCARIAS  |
| `Favorecido.ContaAtualizada`           | PUT `/favorecidos/{doc}/contas/{id}`               | UPSERT                   |
| `Favorecido.ContaRemovida`             | DELETE `/favorecidos/{doc}/contas/{id}`            | soft-delete              |
| `Motorista.Criado`                     | POST `/motoristas`                                 | UPSERT MOTORISTAS_CONDUTORES |
| `Motorista.Atualizado`                 | PUT `/motoristas/{cpf}`                            | UPDATE                   |
| `FavorecidoMotorista.Vinculado`        | POST `/favorecidos/{doc}/motoristas`               | INSERT LINK              |
| `FavorecidoMotorista.Desvinculado`     | DELETE `/favorecidos/{doc}/motoristas/{cpf}`       | UPDATE DesvinculadoEm    |
| `Veiculo.Criado`                       | POST `/veiculos`                                   | UPSERT VEICULOS          |
| `Veiculo.Atualizado`                   | PUT `/veiculos/{placa}`                            | UPDATE                   |
| `Veiculo.Removido`                     | DELETE `/veiculos/{placa}`                         | UPDATE Ativo=false       |
| `Composicao.Criada`                    | POST `/composicoes`                                | INSERT COMPOSICOES+REBOQUES |
| `Composicao.Atualizada`                | PUT `/composicoes/{id}`                            | UPDATE + replace REBOQUES |
| `Composicao.Removida`                  | DELETE `/composicoes/{id}`                         | UPDATE Ativo=false       |

Mais existente: `Ciot.Emitido` (já em produção, Bridge já consome).

---

## Polling Bridge

Bridge consome `GET /api/v1/outbox/pending?size=50` (já existente — `Zenatur.Ciot.Api.Controllers.OutboxController`):

```http
GET /api/v1/outbox/pending?size=50
X-Api-Key: /t68x4gMc4VFDtHVafSycg+x7eoLdRgHVgMaTW+PlMY=
```

Resposta:

```json
[
  {
    "id": 1,
    "messageType": "Motorista.Criado",
    "payload": "{...JSON conforme acima...}",
    "createdAt": "2026-05-14T15:20:00Z",
    "retryCount": 0
  },
  ...
]
```

Após processar, ACK via `POST /api/v1/outbox/ack`:

```json
{ "id": 1, "success": true }
```

Ou falha:

```json
{ "id": 1, "success": false, "errorLog": "Connection timeout to legacy SQL" }
```

Bridge marca `Status=1` (Processed) ou `Status=2` (Failed após `maxRetries`).
