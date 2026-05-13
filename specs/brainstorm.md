Com certeza. Para que uma nova sessão do Claude (ou qualquer desenvolvedor) assuma o **Zenatur Legacy Bridge API** sem ter participado das reuniões anteriores sobre o CIOT, o documento precisa ser um "Manual de Integração" completo, focado em **Isolamento de Domínio** e **Performance com Dapper**.

Aqui está o arquivo pronto para você copiar e colar:

---

# 🏗️ Especificação Técnica: Zenatur Legacy Bridge API

## 1. Visão Geral e Contexto

A **Legacy Bridge API** é um componente de infraestrutura desenhado para atuar como um **Anti-Corruption Layer (ACL)**. Ela isola o novo ecossistema de APIs (CIOT/TMS) do banco de dados SQL Server legado da Zenatur.

Sua missão é permitir que o novo sistema consulte dados históricos (Motoristas/Veículos) e sincronize novos eventos (Emissões de CIOT) sem que a API principal precise conhecer as tabelas ou a estrutura complexa do sistema antigo.

## 2. Stack Tecnológica

* **Runtime:** .NET 8 (LTS).
* **Tipo de Projeto:** Web API (Minimal APIs).
* **Acesso a Dados:** **Dapper** (Uso obrigatório para performance e manipulação de SQL puro).
* **Resiliência:** Polly (Políticas de Retry para conexões com o banco legado).
* **Monitoramento:** Serilog para rastrear falhas de sincronização.

## 3. Arquitetura de Endpoints (Interface Externa)

### 3.1. Queries (Leitura para Autopreenchimento)

Estes endpoints servem para que o Front-end busque dados no legado ao digitar um CPF ou Placa.

* `GET /v1/legacy/motoristas/{cpf}`: Retorna nome, telefone e endereço.
* `GET /v1/legacy/veiculos/{placa}`: Retorna dados do veículo e documento do proprietário.

### 3.2. Commands (Sincronização / Outbox Consumption)

Endpoints usados pelo Worker para persistir dados vindos do sistema de CIOT.

* `POST /v1/legacy/sync/ciot-emitido`: Recebe o payload do CIOT e atualiza as tabelas legadas.
* `POST /v1/legacy/sync/status-parcela`: Atualiza o status de pagamento no legado.

## 4. O Motor de Sincronização (Outbox Worker)

O projeto deve conter um `BackgroundService` que implementa o **Transactional Outbox Pattern** como consumidor.

**Fluxo do Worker:**

1. **Polling:** Realiza chamadas periódicas à API CIOT em `GET /api/v1/outbox/pending`.
2. **Processamento:**
* Identifica o tipo de mensagem (ex: `Ciot.Emitido`).
* Inicia uma transação SQL via Dapper no banco legado.
* Executa os comandos SQL necessários.


3. **Confirmação (ACK):**
* Em caso de sucesso, notifica a API CIOT via `POST /api/v1/outbox/ack`.
* Em caso de erro, envia o log técnico para a API CIOT para registro de falha na tabela original.



## 5. Regras de Engenharia e Boas Práticas

### 5.1. Performance e Concorrência

* **NOLOCK:** Todas as queries de leitura (`SELECT`) devem utilizar obrigatoriamente o hint `WITH (NOLOCK)` para evitar contenção com o sistema legado produtivo.
* **Dapper Mapping:** Utilize Dapper para mapear os nomes de colunas do legado (ex: `NM_MOT`) para propriedades amigáveis (ex: `NomeMotorista`).

### 5.2. Segurança

* **API Key:** As chamadas entre a API CIOT e a Legacy Bridge devem ser autenticadas via `X-Api-Key` no cabeçalho.
* **Isolamento:** Esta API não deve ter acesso à internet pública; deve ser acessível apenas pela rede interna ou VPN.

### 5.3. Idempotência

* O sistema deve garantir que a mesma mensagem da Outbox não seja processada duas vezes no legado. Verifique a existência do identificador único (ID da base nova) antes de realizar inserções.

## 6. Estrutura de Pastas Sugerida

```text
Zenatur.LegacyBridge/
├── Controllers/              # Endpoints da API (Minimal APIs)
├── Services/
│   ├── SyncWorker.cs         # BackgroundService de Polling
│   └── LegacyRepository.cs   # Queries e Commands SQL (Dapper)
├── DTOs/                     # Contratos de Request/Response
└── Program.cs                # Configuração da DI e Middlewares

```
 