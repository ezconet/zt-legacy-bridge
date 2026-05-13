# Zenatur Legacy Bridge — Project Guide

> Anti-Corruption Layer entre novo ecossistema (CIOT API + TMS Portal) e SQL Server legado da Zenatur. **Único componente** autorizado a ler/escrever no banco legado.

## Specs (leitura obrigatória antes de qualquer task)

1. [specs/architecture.md](specs/architecture.md) — spec arquitetural completa, contratos, DDLs, payload `Ciot.Emitido` v1.
2. [specs/tasks.md](specs/tasks.md) — backlog de implementação por fases. **Sequência sugerida:** Fase 0 (bloqueantes) → Fase 1 (esqueleto) → ... → Fase 8 (operação).

## Stack

- **.NET 8 LTS** (C# 12) — mesmo runtime do CIOT-API.
- **ASP.NET Core Minimal APIs** + `BackgroundService` no **mesmo binário** (Web API host + Outbox Worker).
- **Dapper** — único ORM permitido. **EF Core PROIBIDO** (lock-in com schema legado frágil).
- **Polly** — retry transient SQL + Polly em HttpClient.
- **FluentResults** — Regra de Ouro #1 (todos retornos `Result<T>`).
- **FluentValidation** — payload sync.
- **Serilog** JSON rolling file.
- **HealthChecks** AspNetCore (`/health/live`, `/health/ready`).
- **xUnit** + Moq + FluentAssertions + **Testcontainers** (SQL) + **WireMock**.
- **Hosting:** Windows Service (`UseWindowsService()`).

## Estrutura

```
zt-lagacy-bridge/
├── Zenatur.LegacyBridge.sln
├── src/
│   ├── Zenatur.LegacyBridge/                # Web API + Worker host
│   ├── Zenatur.LegacyBridge.Application/    # Ports, Queries, OutboxHandlers, Dispatching, Common
│   └── Zenatur.LegacyBridge.Infrastructure/ # Http (CiotApiClient), Persistence/Dapper, Sql/*.sql
├── tests/
│   ├── Zenatur.LegacyBridge.UnitTests/
│   └── Zenatur.LegacyBridge.IntegrationTests/
├── specs/
│   ├── architecture.md
│   └── tasks.md
└── CLAUDE.md
```

Detalhe completo: `specs/architecture.md` §11.

## Referências entre projetos

```
Web (host) ──► Application + Infrastructure (composition root)
Infrastructure ──► Application
Application ──► (nada — só ports/DTOs)
```

## Regras invioláveis

| Regra | Por quê |
|-------|---------|
| `WITH (NOLOCK)` em **todo** SELECT do legado | Schema legado é produtivo. Sem NOLOCK = contenção/deadlock. Code review bloqueia ausência. |
| **Nada de EF Core** no Bridge | Schema legado é frágil/proprietário (`NM_MOT`, `NR_CGC`...). EF acopla migrations. Dapper + SQL puro. |
| `X-Api-Key` em **toda** chamada inbound + outbound | Auth única entre Bridge ↔ CIOT API ↔ TMS. Sem header → 401 + log Warn `ApiKeyMissing`. |
| Bridge é **único writer** de `dbo.LEGACY_BRIDGE_PROCESSED` | Tabela de dedupe centralizada. PK em `OutboxId`. |
| `schemaVersion` obrigatório no payload outbox | Versão desconhecida → DLQ após retry. Payload `Ciot.Emitido` é **v1 CONGELADO**. |
| PII mascarado em logs | CPF `123******01`, CNPJ idem. **Nunca** log com documento completo. |
| Web API + Worker no **mesmo processo** | Repos chamados via DI, não HTTP. Se um dia separar, contratos já existem. |
| Todo método de Application retorna `Result<T>` (FluentResults) | Não joga exception em fluxo de negócio. Exception só em bug. |
| Polly retry transient em SQL: `SqlException.Number` ∈ {1205, 4060, 40197, 40501, 40613, 49918–49920} | Padrão legacy SQL. 3x exponencial (1s/2s/4s). |
| SQL embeds em `Infrastructure/Persistence/Dapper/Sql/*.sql` | Versionável, revisável. Não strings inline. |
| Datas UTC ISO 8601 no payload. Conversão p/ `E. South America Standard Time` ao gravar legado | Legado guarda horário local. |
| Documentos digits only (sem máscara) | FluentValidation rejeita letras/máscara. |
| Decimais `DbType.Decimal` precision 18 scale 2 | Sem float, sem vírgula. Sempre ponto. |

## Auth flow

- **Inbound** (TMS → Bridge): `ApiKeyMiddleware` valida header `X-Api-Key` contra `InboundApiKey` da config. Aplicado globalmente exceto `/health/*`.
- **Outbound** (Bridge → CIOT API): `ApiKeyDelegatingHandler` injeta header `X-Api-Key` (config `CiotApi:ApiKey`).
- Mesma chave física Bridge↔CIOT. Chave gerada via `RandomNumberGenerator` 32 bytes Base64. Env var em prod.

## Config

`appsettings.json`:

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

Env vars prod: `CiotApi__ApiKey`, `LegacyDb__ConnectionString`, `InboundApiKey`, `ASPNETCORE_ENVIRONMENT=Production`.

## Comandos comuns

```powershell
# Build
dotnet build

# Run local (Development)
dotnet run --project src/Zenatur.LegacyBridge

# Run tests
dotnet test

# Run apenas integration
dotnet test tests/Zenatur.LegacyBridge.IntegrationTests

# EF Migrations: N/A (proibido)
```

## Health endpoints

- `GET /health/live` — processo vivo (200 sempre).
- `GET /health/ready` — Legacy DB `SELECT 1` + CIOT API alcançável.

## Cobertura mínima

- `Application/`: 90%.
- `Infrastructure/Persistence/Dapper/`: 70% (SQL puro coberto por integration tests).

## Pendências externas (bloqueantes da Fase 0)

Ver `specs/tasks.md` Fase 0:
- **B1** Inventário real schema legado (mantém DDL gabarito de §10-bis até confirmar).
- **B2** Connection string DEV legado.
- **B3** Chave `X-Api-Key` acordada.
- **B4** Criar `dbo.LEGACY_BRIDGE_PROCESSED` em DEV/HML/PRD.
- **B5** Congelar payload v1 lado CIOT-API (`schemaVersion: 1`).

## Repos relacionados

| Repo | Path local | Papel |
|------|-----------|-------|
| `zt-ciot-api` | `D:\devops.zenatur\zt-ciot-api` | API CIOT (origem dos eventos outbox). |
| `zt-tms-web` | `D:\devops.zenatur\zt-tms-web` | TMS Portal Blazor (consumidor Query API Bridge). |

Bridge **não** depende de código fonte desses repos — só dos contratos HTTP embutidos em `specs/architecture.md` §8 e §9.

## Glossário rápido

- **Bridge:** este projeto.
- **ACL:** Anti-Corruption Layer.
- **Outbox:** `Queue.OutboxMessages` no banco da CIOT API (`ZT_TMS_CIOT`).
- **Dedupe table:** `dbo.LEGACY_BRIDGE_PROCESSED` no banco legado.
- **DLQ lógica:** `Status = Failed` no `Queue.OutboxMessages` (replay manual via SQL).
- **schemaVersion:** inteiro no payload JSON; handler rejeita versão não suportada.
