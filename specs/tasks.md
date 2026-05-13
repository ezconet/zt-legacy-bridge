# Tarefas: Zenatur Legacy Bridge

> Projeto destino: `D:\devops.zenatur\zt-lagacy-bridge`
> Spec base: [architecture.md](architecture.md)
> Stack: .NET 8 LTS · Minimal APIs · BackgroundService · Dapper · Polly · Serilog · FluentResults · FluentValidation · Windows Service

---

## Fase 0 — Bloqueantes (input externo)

- [ ] **B1** — Inventário schema legado: `MOTORISTAS`, `VEICULOS`, `PROPRIETARIOS`, `CIOT_EMISSAO`, `CIOT_PARCELAS` (nomes reais, colunas, PKs, índices). Sem isso, repos Dapper são chute.
- [ ] **B2** — Connection string DEV do SQL Server legado.
- [ ] **B3** — Definir e armazenar `X-Api-Key` (mesma chave usada por Bridge ↔ CIOT API ↔ TMS). Gerar via `RandomNumberGenerator` (32 bytes Base64).
- [ ] **B4** — Criar `dbo.LEGACY_BRIDGE_PROCESSED` em DEV/HML/PRD legado (DDL §4.1 da architecture).
- [ ] **B5** — Congelar payload `Ciot.Emitido` v1 lado CIOT-API: adicionar `schemaVersion: 1` em `InsertFreightContractCommandHandler` antes de chamar `IOutboxService.EnqueueAsync`.

---

## Fase 1 — Esqueleto da Solution

- [x] **L1** — Criar Solution `Zenatur.LegacyBridge.sln` em `D:\devops.zenatur\zt-lagacy-bridge` com 3 projetos: `Zenatur.LegacyBridge` (Web API host), `Zenatur.LegacyBridge.Application`, `Zenatur.LegacyBridge.Infrastructure` + 2 projetos de teste (`UnitTests`, `IntegrationTests`).
- [x] **L2** — Referências: Web→Application + Infrastructure (composition root); Infrastructure→Application; Application sem dependências externas (pure ports/DTOs).
- [x] **L3** — Adicionar NuGets: `Dapper`, `Microsoft.Data.SqlClient`, `Polly`, `Polly.Extensions.Http`, `Serilog.AspNetCore`, `Serilog.Sinks.File`, `FluentResults`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `Microsoft.Extensions.Hosting.WindowsServices`, `AspNetCore.HealthChecks`, `Microsoft.Extensions.Http.Polly`.
- [x] **L4** — `Program.cs`: `WebApplicationBuilder` + `UseWindowsService()` + Serilog JSON rolling file + DI vazio + Health endpoints (`/health/live`, `/health/ready`).
- [x] **L5** — Estrutura de pastas conforme §11 da architecture (Endpoints, Middleware, Workers, HealthChecks no host; Ports, Queries, OutboxHandlers, Dispatching, Common na Application; Http, Persistence/Dapper, Sql/ na Infrastructure).
- [x] **L6** — `appsettings.json` + `appsettings.Development.json` com seções `CiotApi`, `LegacyDb`, `InboundApiKey`, `Serilog` (§12.1).

---

## Fase 2 — Auth Inbound (X-Api-Key)

- [ ] **L7** — `ApiKeyMiddleware` no host: lê `InboundApiKey` da config, valida header `X-Api-Key`, rejeita 401 com log `ApiKeyMissing` (Warn).
- [ ] **L8** — Aplicar middleware globalmente exceto em `/health/*`.
- [ ] **L9** — Teste integration: `WebApplicationFactory` valida 401 sem header, 401 com header errado, 200 com header correto em endpoint stub.

---

## Fase 3 — Query API: Motoristas

- [ ] **L10** — Port `IMotoristaRepository` em Application/Ports + DTO `MotoristaDto` (CPF, nome, dataNascimento, telefone, endereco{}, rntrc{}).
- [ ] **L11** — `DapperConnectionFactory` em Infrastructure/Persistence/Dapper (scoped, lê `LegacyDb:ConnectionString`, `CommandTimeoutSeconds`).
- [ ] **L12** — `MotoristaRepository` Dapper: SQL `WITH (NOLOCK)` em arquivo embed `Sql/motorista_by_cpf.sql`. Polly retry transient (SqlException 1205, 4060, 40197, 40501, 40613, 49918–49920) — 3x exponencial.
- [ ] **L13** — `GetMotoristaByCpfQuery` + `Handler` (Application/Queries): sanitiza CPF (digits only), chama repo, retorna `Result<MotoristaDto>` (Fail se não encontrado).
- [ ] **L14** — `MotoristasEndpoints.MapGroup("/v1/legacy/motoristas")` com `GET /{cpf}`: 200 payload, 404 `{ error, cpf }`, 503 com `Retry-After` se DB offline.
- [ ] **L15** — Mascarar CPF em logs (`123******01`).
- [ ] **L16** — Unit tests: handler com repo mock (sanitização, 200, 404). Integration test: WebApplicationFactory + Testcontainers SQL com seed mínimo.

---

## Fase 4 — Query API: Veículos

- [ ] **L17** — Port `IVeiculoRepository` + DTO `VeiculoDto` (placa, tipoVeiculo, renavam, anoFabricacao, anoModelo, marca, modelo, tara, capacidadeKg, proprietario{}).
- [ ] **L18** — `VeiculoRepository` Dapper + SQL `Sql/veiculo_by_placa.sql` `WITH (NOLOCK)` + retry Polly.
- [ ] **L19** — `GetVeiculoByPlacaQuery` + Handler: normaliza placa (uppercase, sem hífen, valida Mercosul ou antigo).
- [ ] **L20** — `VeiculosEndpoints` com `GET /v1/legacy/veiculos/{placa}`: 200/404/503.
- [ ] **L21** — Unit + integration tests.

---

## Fase 5 — Outbox Worker (esqueleto, sem persistência sync)

- [ ] **L22** — `ICiotApiClient` em Application/Ports: `GetPendingAsync(int size, CancellationToken)`, `AckAsync(AckRequest, CancellationToken)`, ambos retornam `Result<T>`.
- [ ] **L23** — `CiotApiClient` em Infrastructure/Http: `HttpClientFactory` tipado + `ApiKeyDelegatingHandler` (injeta `X-Api-Key` outbound) + Polly retry 3x (1s/2s/4s) + circuit breaker + timeout 30s.
- [ ] **L24** — `OutboxMessageDto` + `AckRequest` em Application/Common (espelha contratos `outbox-sql.md`).
- [ ] **L25** — Port `IOutboxMessageHandler` (`MessageType`, `SupportedSchemaVersion`, `HandleAsync`).
- [ ] **L26** — `OutboxDispatcher` em Application/Dispatching: dicionário `MessageType → Handler`, valida `schemaVersion` do payload, retorna Fail em handler ausente / versão errada.
- [ ] **L27** — `CiotEmitidoHandler` stub: retorna `Result.Ok()` (no-op) — só pra fechar loop.
- [ ] **L28** — `OutboxPollingWorker : BackgroundService`: loop conforme §7.1 (poll → dispatch → ack). Configs `BatchSize`, `PollIntervalIdleMs`, `PollIntervalBusyMs`, `BackoffMsOnFailure` da seção `CiotApi`.
- [ ] **L29** — Unit tests: `OutboxDispatcher` (tipo desconhecido, versão errada, success/fail). `CiotApiClient` com `HttpMessageHandler` mock (200/401/500/timeout/retry exhaustion).

---

## Fase 6 — Persistência Sync (Ciot.Emitido v1)

- [ ] **L30** — Port `ILegacyCiotRepository` (`UpsertCiotEmitidoAsync(payload, IDbTransaction)`, `InsertParcelasAsync(...)`).
- [ ] **L31** — `LegacyCiotRepository` Dapper + SQL embed: `insert_ciot_emissao.sql`, `insert_ciot_parcelas.sql`, `upsert_dedupe.sql`. Datas UTC → `E. South America Standard Time` (helper). Decimais `DbType.Decimal(18,2)`.
- [ ] **L32** — `CiotEmitidoPayload` (DTO) + `CiotEmitidoPayloadValidator` (FluentValidation): documentos digits only, decimais ≥ 0, parcelas não vazias, datas válidas.
- [ ] **L33** — `CiotEmitidoMapper`: payload → parâmetros Dapper.
- [ ] **L34** — `CiotEmitidoHandler` real (substitui stub L27):
  - `BeginTransaction(ReadCommitted)`
  - `INSERT LEGACY_BRIDGE_PROCESSED` (id, type) — catch SqlException 2627/2601 → `Result.Ok()` (já processada)
  - validar payload (FluentValidation)
  - validar `schemaVersion == 1`
  - `UpsertCiotEmitidoAsync` + `InsertParcelasAsync`
  - `Commit` → `Result.Ok()`
  - Polly retry transient para deadlock 1205 (3x)
- [ ] **L35** — Unit tests `CiotEmitidoHandler`: happy path, dedupe-hit (SqlException 2627), payload inválido, schemaVersion mismatch, deadlock retry exhaustion.
- [ ] **L36** — Integration test `OutboxFlowTests`: WireMock CIOT (`/outbox/pending` + `/outbox/ack`) + Testcontainers SQL com schema mínimo (`MOTORISTAS`, `CIOT_EMISSAO`, `CIOT_PARCELAS`, `LEGACY_BRIDGE_PROCESSED`). Cenários: happy E2E, ACK falha pós-write (verifica idempotência no próximo ciclo), legacy DB deadlock, mensagem duplicada.

---

## Fase 7 — Health & Observabilidade

- [ ] **L37** — `LegacyDbHealthCheck`: `SELECT 1` no legado.
- [ ] **L38** — `CiotApiHealthCheck`: HEAD/GET trivial em endpoint público da CIOT API.
- [ ] **L39** — `/health/ready` agrega ambos; `/health/live` retorna 200 sempre.
- [ ] **L40** — Logs estruturados Serilog: enrichers `outboxId`, `messageType`, `schemaVersion`, `retryCount`, `durationMs`, `cpfMasked`, `placa`. Eventos da §14.2.

---

## Fase 8 — Operação (Windows Service + Deploy)

- [ ] **L41** — `install-service.ps1`: `sc.exe create ZenaturLegacyBridge binPath= ... start= auto` + `sc.exe failure ... reset= 86400 actions= restart/60000/restart/60000/restart/60000`.
- [ ] **L42** — `uninstall-service.ps1` + `restart-service.ps1`.
- [ ] **L43** — Runbook `OPERATIONS.md`: como replay de `Failed` (SQL update), troubleshooting comum, leitura de log JSON, métricas extraíveis.
- [ ] **L44** — README do repo: pré-reqs, build, run local, run como service, configuração env vars.

---

## Fase 9 — Mudanças em Outros Repos (consequências da spec)

- [ ] **X1** — `zt-ciot-api`: `OutboxController` exige `X-Api-Key` (middleware ou filter). Chave em `LegacyBridge:ApiKey` no `appsettings`.
- [ ] **X2** — `zt-ciot-api`: `InsertFreightContractCommandHandler` adiciona `schemaVersion: 1` ao payload `Ciot.Emitido`.
- [ ] **X3** — `zt-ciot-api`: atualizar `specs/outbox-sql.md` (envelope `schemaVersion`, header `X-Api-Key` nos 2 endpoints).
- [ ] **X4** — `zt-tms-web`: criar `LegacyBridgeClient` (HttpClient tipado) em Infrastructure + DI + config `LegacyBridge:BaseUrl/ApiKey`.
- [ ] **X5** — `zt-tms-web`: T11 (CiotStepper Fase 1) — incluir `bridgeTask` no `Task.WhenAll` (3 fontes: CIOT API + Pamcard + Bridge). Prioridade merge: cadastrais=Bridge, RNTRC/ANTT=Pamcard, histórico novo=CIOT.
- [ ] **X6** — `specs/tms/ciot-module.md` §3 Fase 1: documentar 3 fontes.
- [ ] **X7** — Memory `project_state.md` (TMS): atualizar nota de T11 para mencionar Bridge.

---

## Critérios de Aceite Globais

- Cobertura: Application 90%, Infrastructure/Persistence/Dapper 70%.
- `WITH (NOLOCK)` em **todo** SELECT do legado. Code review bloqueia ausência.
- Bridge é **único writer** de `LEGACY_BRIDGE_PROCESSED`.
- Nenhum CPF/CNPJ completo aparece em log.
- Deploy = Windows Service auto-start com `RestartOnFailure`.
- Worker e endpoints HTTP rodam no **mesmo processo / mesmo binário**.
- Proibido EF Core no Bridge.

---

## Legenda

`[ ]` Pendente | `[~]` Em andamento | `[x]` Concluído
Prefixos: **B**=Bloqueante externo · **L**=LegacyBridge interno · **X**=Mudança em outro repo
