# CLAUDE.md

Este arquivo guia o Claude Code ao trabalhar neste repositório. Mantenha-o atualizado conforme o projeto evoluir.

## Visão Geral do Projeto

Um **MCP Server** (Model Context Protocol) financeiro, exposto via **custom connector**, que permite ao Claude (via chat/Claude.ai, Claude Desktop, Claude Code etc.) **consultar, inserir e modificar** transações financeiras de:

- **Conta Corrente (CC)** — extratos bancários
- **Cartão de Crédito (CD)** — faturas de cartão (Nubank, Bradesco, Sofisa, e futuros)

O MCP também expõe ferramentas para **metas de orçamento** (`Meta_Valor` por categoria/mês) e **projeção de saldo**, aplicando as regras de negócio descritas na seção [Regras de Negócio](#regras-de-negócio-do-domínio-financeiro) abaixo — essas regras são a fonte de verdade para qualquer lógica de cálculo implementada no backend e **devem** ser mantidas sincronizadas com o comportamento real do código.

Construído com:
- **Backend:** .NET 10, orquestrado via .NET Aspire
- **Orquestração:** .NET Aspire AppHost para desenvolvimento local, service discovery e observabilidade
- **Persistência:** PostgreSQL (via EF Core + Npgsql), provisionado como recurso Aspire (`AddPostgres`/`AddDatabase`)
- **Auth:** provedor JWT customizado (emissão e validação próprias em `FinancialMcp.Api`/`FinancialMcp.Application`, sem Identity/Entra ID)
- **Protocolo:** MCP (Model Context Protocol) sobre o mesmo host da API, autenticado via JWT bearer

> Documentação relacionada (fora deste arquivo) pode ser referenciada livremente por link relativo — ver [Vinculando Outros Artefatos e Docs](#vinculando-outros-artefatos-e-docs).

## Estrutura da Solução

```
/FinancialMcp.sln
/src
  /FinancialMcp.AppHost            -> Projeto de orquestração Aspire (entry point para `dotnet run`)
  /FinancialMcp.ServiceDefaults    -> Defaults compartilhados do Aspire (health checks, telemetria, resiliência)
  /FinancialMcp.Api                -> ASP.NET Core Web API + MCP Server host (+ SignalR Hub, se necessário para updates em tempo real)
  /FinancialMcp.Application        -> Lógica de aplicação/negócio (use cases, services, ferramentas MCP)
  /FinancialMcp.Domain              -> Entidades de domínio, value objects (Transação, Conta, Cartão, MetaOrçamento)
  /FinancialMcp.Infrastructure      -> EF Core, persistência, importação/parsing de extratos CSV, integrações externas

/tests
  /FinancialMcp.Api.Tests
  /FinancialMcp.Application.Tests
```

## Comandos Comuns

### Rodar tudo (backend + Aspire dashboard)
```bash
dotnet run --project src/FinancialMcp.AppHost
```
O Aspire dashboard abre automaticamente (default: https://localhost:17090) mostrando logs, traces e métricas de todos os serviços, incluindo as chamadas de ferramentas MCP.

### Rodar testes
```bash
dotnet test
```

### Build
```bash
dotnet build
```

### Adicionar uma migration do EF Core (Postgres)
```bash
dotnet ef migrations add <Name> --project src/FinancialMcp.Infrastructure --startup-project src/FinancialMcp.Api
```
A connection string do Postgres é injetada pelo Aspire (variável `ConnectionStrings__financialmcp-db`); não hardcodar host/porta/credenciais em `appsettings.json` — apenas defaults locais de desenvolvimento, se necessário.

## Arquitetura

### MCP
- **Auth:** conexões MCP usam o mesmo esquema JWT bearer da REST API (provedor customizado, ver [Autenticação](#autenticação-jwt-customizado)); token passado via `accessTokenFactory` no client.
- **Ferramentas expostas (tools)** — nomes sugeridos, ajustar conforme implementação real:
  - `list_transactions` — lista transações de CC e/ou CD com filtros (tipo, status, categoria/subcategoria, conta, cartão, período, mês de referência).
  - `get_transaction` — detalhe de uma transação específica.
  - `create_transaction` — insere uma nova transação (CC ou CD), respeitando os campos obrigatórios de cada extrato.
  - `update_transaction` — altera campos de uma transação existente (ex.: status, categoria, valor, data).
  - `delete_transaction` — remove uma transação, deve ser soft delete. **Operação destrutiva**: exigir confirmação explícita do chamador antes de executar.
  - `reconcile_transaction` — marca uma transação como `Conciliado` (CC) ou equivalente em CD.
  - `list_categories` — lista categorias-mãe e subcategorias em uso (parse de `Categoria-mãe/Subcategoria`).
  - `get_budget_status` — calcula `Gasto_Real`, `Saldo_Meta` e `% Utilizado` por categoria/mês, conforme `metas_orcamento.csv` (ver regras abaixo).
  - `get_balance_projection` — gera a projeção de saldo consolidada (`projecao_saldo_contas_completo.csv`), aplicando o ciclo de fatura, parcelamento e lançamentos fixos.
  - `import_statement` — importa um novo extrato CSV (CC ou CD) para a base.
- Toda ferramenta que **grava/altera** dados (create/update/delete/import) deve validar os campos de acordo com o formato de origem (separador `;`, datas `dd/mm/aaaa`, decimal com ponto) antes de persistir.

### Aspire
- `FinancialMcp.AppHost/AppHost.cs` define os recursos: Postgres (`AddPostgres("financialmcp-postgres").AddDatabase("financialmcp-db")`), projeto API (`.WithReference(postgres)`), Redis se necessário para cache, app React se houver (como recurso `npm`).
- `FinancialMcp.ServiceDefaults` configura OpenTelemetry, health checks (`/health`, `/alive`) e handlers de resiliência padrão — referenciar em todo projeto de serviço.
- Service discovery: referenciar outros serviços pelo nome do recurso Aspire (ex.: `https+http://financialmcp-api`), nunca URLs hardcoded.
- Em produção, o recurso Postgres do AppHost é substituído pela connection string real via configuração de ambiente/secret — o dashboard local nunca deve apontar para banco de produção.

### Persistência (Postgres)
- **Provider:** EF Core com `Npgsql.EntityFrameworkCore.PostgreSQL`, `DbContext` em `FinancialMcp.Infrastructure`.
- **Schema/Migrations:** todas as migrations do EF Core ficam versionadas em `FinancialMcp.Infrastructure/Migrations`; nunca editar uma migration já aplicada em ambiente compartilhado — criar uma nova.
- **Tipos:** mapear `Valor`/`Meta_Valor`/`Gasto_Real`/`Saldo_Meta` como `numeric` (não `double precision`); datas (`Data prevista`, `Data efetiva`, `Data Conciliado`, `Venc. Fatura`) como `date`/`timestamptz` conforme o caso, nunca `text`.
- **Índices:** garantir índice em `(Mês_Ano, CategoriaMae)` e em `(Status, Tipo)` para acelerar as agregações de `get_budget_status`; índice em `Cartão`/`ParcelaTotal` para as queries de `get_balance_projection`.
- **Soft delete:** `delete_transaction` seta uma coluna `DeletedAt`/`IsDeleted`; um global query filter no `DbContext` exclui registros deletados de todas as queries por padrão (queries administrativas explícitas podem ignorar o filtro via `IgnoreQueryFilters()`).
- **Connection string:** sempre via Aspire service discovery/`ConnectionStrings`, nunca hardcoded (ver [Comandos Comuns](#comandos-comuns)).

### Autenticação (JWT customizado)
- **Emissão:** endpoint próprio (ex.: `POST /auth/token`) em `FinancialMcp.Api`, valida credenciais e emite um JWT assinado (HMAC ou RSA, chave via configuração/secret do Aspire) com claims mínimas (`sub`, `exp`, `iat`, e claims de escopo/role necessárias para diferenciar operações de leitura vs. escrita nas tools MCP).
- **Validação:** middleware padrão do ASP.NET Core (`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`), configurado para validar issuer, audience, tempo de vida e assinatura; a mesma configuração de validação é reutilizada pelo host MCP (mesmo pipeline de auth da REST API, não um esquema paralelo).
- **Refresh:** se necessário, refresh token opaco persistido no Postgres (tabela própria, nunca reaproveitando a tabela de usuários diretamente), com expiração curta para o access token e rotação do refresh token a cada uso.
- **Segredos:** chave de assinatura e parâmetros sensíveis nunca ficam em `appsettings.json` versionado — usar `dotnet user-secrets` localmente e configuração de ambiente/secret manager em produção.
- **Escopo de uso:** este é o único mecanismo de auth do projeto (sem ASP.NET Core Identity, sem Entra ID); qualquer necessidade futura de login federado deve ser tratada como uma extensão deste provedor, não uma substituição silenciosa.

## Regras de Negócio do Domínio Financeiro

Estas regras **governam** o comportamento das ferramentas MCP de consulta e cálculo (`get_budget_status`, `get_balance_projection`, `list_transactions`, etc.). Qualquer mudança de comportamento no código deve manter este documento atualizado.

### Categoria e subcategoria
- A coluna `Categoria` dos extratos (CC e CD) é tratada como `Categoria-mãe/Subcategoria`, feito o split pelo caractere `/` quando presente.
- Ao agregar por uma meta lançada apenas com a categoria-mãe (ex.: `Moradia`), somar **todas** as linhas cuja categoria-mãe seja `Moradia`, independente da subcategoria.
- Gerar também um detalhamento por subcategoria (secundário, não cria meta própria).
- Se o usuário cadastrar a meta já com subcategoria completa (ex.: `Moradia/Seguro`), o cálculo passa a ser exato para aquela subcategoria específica.

### Metas de orçamento (`get_budget_status`)
1. **Filtro de status** (padrão, salvo indicação contrária): apenas `Status = Conciliado`. Ignorar `Agendado` (CC) e `Nconciliado` (CD).
2. **Filtro de tipo**: apenas `Tipo = Despesa`. Não incluir `Receita` por padrão. Nunca incluir `Transferência` nem `Pagamento` (o "Pagamento de cartão" na conta corrente é excluído para não duplicar o gasto já contado via lançamentos do cartão).
3. **Data de referência do mês (`Mês_Ano`)**:
   - Conta Corrente (quando `Conciliado`): usar a coluna **"Data Conciliado"**.
   - Cartão de Crédito: usar a coluna **"Venc. Fatura"** (não "Data efetiva") — reflete o mês em que o valor efetivamente impacta a conta de pagamento, respeitando a regra de virar para o próximo dia útil quando o vencimento cair em fim de semana.
4. **Fórmulas**:
   - `Gasto_Real` (padrão, salvo indicação contrária) = soma do valor absoluto das despesas conciliadas da mesma categoria-mãe (ou categoria completa, se a meta especificar subcategoria) no mesmo `Mês_Ano` (CC + CD combinados).
   - `Saldo_Meta` = `Meta_Valor` − `Gasto_Real`.
   - `% Utilizado` = `Gasto_Real` / `Meta_Valor`.
5. Categorias sem meta cadastrada não entram na planilha/consulta de metas (mas podem aparecer em um relatório à parte, se solicitado via `list_transactions`).

### Cartão de crédito — ciclo de fatura, parcelamento e projeção (`get_balance_projection`)
1. **Ciclo da fatura**: compras até o fechamento entram na fatura do mês corrente (vencimento no mês seguinte); compras após o fechamento entram só na fatura seguinte. Usar a coluna **"Venc. Fatura"** (quando existente) como a data que efetivamente impacta o saldo da conta de pagamento — nunca a "Data prevista" da compra.
2. **Lançamentos parcelados**: identificados por `Repetição = "Parcelado"` e pelas colunas `Parcela Atual`/`Parcela Total` (ex.: `6/12`). Cada linha do extrato já representa uma parcela específica — não recalcular nem duplicar parcelas futuras. Ao projetar meses futuros ainda não presentes no extrato, gerar as parcelas restantes (`parcela_atual+1` até `parcela_total`) com vencimento em +1 mês por linha, mesmo valor, mesma descrição-base (sem sufixo `N/M`) e mesmo Cartão/Conta.
3. **Lançamentos fixos mensais**: `Repetição = "Fixo Mês"` — repetir o mesmo valor todo mês na mesma data de vencimento até indicação de término.
4. **Consolidação no saldo da conta**: o extrato de cartão **não** deve ser somado diretamente ao saldo da conta corrente. O valor total da fatura (soma dos lançamentos com "Venc. Fatura" no mesmo mês) deve aparecer como um único lançamento "Pagamento de cartão" na conta corrente, na data de vencimento, debitando da "Conta" vinculada ao cartão. Se o vencimento cair em sábado/domingo, considerar o próximo dia útil.
5. **Status**: `Conciliado` = já processado/confirmado; `Nconciliado` = previsto, ainda sujeito a alteração de valor/data até o fechamento da fatura.

## Convenções de Código

- **C#:** Nullable reference types habilitado, namespaces file-scoped, primary constructors quando melhoram a clareza.
- **Async:** todos os métodos I/O-bound são `async`/`await`; sufixo `Async`.
- **DTOs:** nunca expor entidades de domínio diretamente via MCP/SignalR/REST — mapear para DTOs/records.
- **Dinheiro:** usar `decimal` (nunca `double`/`float`) para `Valor`, `Meta_Valor`, `Gasto_Real`, `Saldo_Meta`.
- **Datas:** tratar `Data prevista`, `Data efetiva`, `Data Conciliado` e `Venc. Fatura` como tipos de data explícitos (não string); centralizar a lógica de "próximo dia útil" em um helper único, reutilizado por `get_balance_projection` e `get_budget_status`.
- **Categoria/Subcategoria:** centralizar o parsing `Categoria-mãe/Subcategoria` em um único helper/value object, reutilizado por todas as ferramentas MCP que agregam por categoria.
- **Validação:** FluentValidation para request/command validation em `FinancialMcp.Application`, aplicada via pipeline behavior do MediatR (ver abaixo), incluindo validação de formato de extrato na importação (`import_statement`).
- **Nomenclatura:** métodos em PascalCase no lado C#, camelCase no lado cliente (`ReceiveMessage` ↔ `receiveMessage`).

### Padrão Mediator (MediatR)

Todas as ferramentas MCP e endpoints REST devem ser **thin**: apenas montam o `IRequest`/`IRequest<TResponse>` e chamam `IMediator.Send(...)` (ou `Publish` para notifications). Nenhuma regra de negócio deve viver na tool/handler MCP nem no controller — a lógica pertence aos handlers do MediatR em `FinancialMcp.Application`.

- **CQRS explícito:** separar sempre em **Commands** (escrita: `create_transaction`, `update_transaction`, `delete_transaction`, `reconcile_transaction`, `import_statement`) e **Queries** (leitura: `list_transactions`, `get_transaction`, `list_categories`, `get_budget_status`, `get_balance_projection`).
- **Organização por feature:** agrupar cada request + handler + validator (+ DTO de resposta) na mesma pasta de feature, não em pastas genéricas `Commands/`, `Queries/`, `Handlers/` soltas:
  ```
  FinancialMcp.Application/
    Transactions/
      CreateTransaction/
        CreateTransactionCommand.cs        (record, implementa IRequest<TransactionDto>)
        CreateTransactionCommandHandler.cs  (IRequestHandler<CreateTransactionCommand, TransactionDto>)
        CreateTransactionCommandValidator.cs (AbstractValidator<CreateTransactionCommand>)
      DeleteTransaction/
        DeleteTransactionCommand.cs
        DeleteTransactionCommandHandler.cs
      ListTransactions/
        ListTransactionsQuery.cs
        ListTransactionsQueryHandler.cs
    BudgetGoals/
      GetBudgetStatus/
        GetBudgetStatusQuery.cs
        GetBudgetStatusQueryHandler.cs
  ```
- **Nomenclatura:** `<Ação><Entidade>Command`/`Query` para o request, `<Nome>Handler` para o handler, `<Nome>Validator` para o FluentValidation validator. Requests são `record` imutáveis; nunca reaproveitar entidades de domínio como request.
- **Um handler por request:** cada `IRequestHandler<TRequest, TResponse>` deve ser a única unidade que orquestra repositórios/services para aquele caso de uso. Regras de cálculo puras (parcelamento, ciclo de fatura, próximo dia útil, agregação de categoria) ficam em services de domínio/aplicação injetados no handler — não escritas inline no handler — para permitir teste unitário isolado (ver [Orientações de Teste](#orientações-de-teste)).
- **Pipeline behaviors** (registrados uma única vez em `FinancialMcp.Application`, via `AddMediatR` + `AddTransient(typeof(IPipelineBehavior<,>), ...)`), na ordem:
  1. `LoggingBehavior<TRequest,TResponse>` — loga request/response (sem dados sensíveis) e integra com o tracing do Aspire/OpenTelemetry.
  2. `ValidationBehavior<TRequest,TResponse>` — executa todos os `IValidator<TRequest>` (FluentValidation) antes do handler; lança `ValidationException` customizada em caso de falha, mapeada para erro MCP/HTTP apropriado.
  3. `TransactionBehavior<TRequest,TResponse>` (apenas para Commands que gravam via EF Core) — abre transação de banco, executa o handler, faz commit/rollback.
- **Notifications (`INotification`)** para efeitos colaterais desacoplados do fluxo principal, sem acoplar o handler de escrita a lógica de outros módulos:
  - Ex.: `TransactionReconciledNotification`, publicada por `ReconcileTransactionCommandHandler`, consumida por um `INotificationHandler` que recalcula `get_budget_status` em cache ou notifica clientes via SignalR.
  - Nunca usar `Publish` para lógica que precisa de retorno síncrono ou que é parte obrigatória da regra de negócio — isso continua sendo responsabilidade do `Command`/`Handler` principal.
- **Operações destrutivas:** `DeleteTransactionCommand` deve carregar um campo explícito de confirmação (ex.: `Confirm: bool`) validado pelo `ValidationBehavior`; o handler rejeita a execução se `Confirm != true`, reforçando a regra de "nunca executar sem confirmação explícita" (ver [O que o Claude Deve Evitar](#o-que-o-claude-deve-evitar)).
- **Registro:** `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FinancialMcp.Application.AssemblyMarker).Assembly))` centralizado em `FinancialMcp.Application`, referenciado a partir de `FinancialMcp.Api` — nunca registrar assemblies MediatR diretamente na camada de API.

## Orientações de Teste

- Testar unitariamente as regras de negócio isoladas dos handlers/tools MCP (extraí-las para services; tools/handlers devem ficar finos).
- Cobrir especificamente com testes:
  - Cálculo de parcelas restantes ao projetar meses futuros.
  - Ciclo de fechamento/vencimento de fatura, incluindo virada para o próximo dia útil em fins de semana.
  - Agregação de `Gasto_Real` por categoria-mãe vs. subcategoria completa.
  - Exclusão de `Transferência`, `Pagamento` e `Receita` do cálculo de metas.
  - Consolidação do "Pagamento de cartão" único na conta corrente, evitando duplicidade de gasto.
- Usar `TestServer` + client MCP real (ou equivalente) para testes de integração das ferramentas expostas.
- Frontend (se houver): React Testing Library para componentes, mockando a conexão MCP/SignalR em vez de abrir sockets reais.

## Vinculando Outros Artefatos e Docs

Este `CLAUDE.md` é o ponto de entrada, mas não precisa concentrar tudo — é permitido e incentivado linkar outros artefatos (docs de arquitetura, ADRs, diagramas, specs de API, outros `CLAUDE.md` de subpastas) sem que isso quebre o parsing ou a navegação:

- **Links relativos ao repositório:** sempre usar caminho relativo a partir da raiz ou da pasta atual (ex.: `docs/adr/0001-postgres.md`, `../FinancialMcp.Infrastructure/README.md`), nunca caminho absoluto de máquina local.
- **Import do Claude Code (`@caminho`):** para conteúdo que deve ser carregado automaticamente como contexto (não só como link clicável), usar a sintaxe `@docs/arquivo.md` em vez de reescrever o conteúdo aqui. Evitar imports circulares (A importa B que importa A) e evitar importar arquivos muito grandes sem necessidade — preferir um resumo aqui + link/import para o detalhe.
- **`CLAUDE.md` aninhados:** subpastas (ex.: `src/FinancialMcp.Infrastructure/CLAUDE.md`) podem existir com regras específicas daquele módulo; este arquivo raiz não precisa duplicá-las, apenas apontar para elas quando relevante.
- **Âncoras estáveis:** ao linkar para uma seção específica deste arquivo (âncora tipo `#autenticação-jwt-customizado`), preferir sempre o título em português tal como está escrito, para não quebrar o link em caso de reordenação de seções.
- **Artefatos externos ao repo** (Notion, Confluence, dashboards do Aspire, Grafana etc.): linkar normalmente em markdown padrão; não é necessário validar disponibilidade desses links durante o build/CI.
- Um link quebrado ou artefato ainda não criado **não** deve bloquear a leitura/uso deste `CLAUDE.md` pelo Claude — tratar como referência opcional, não como dependência obrigatória.

## O que o Claude Deve Evitar

- Não hardcodar portas/URLs — deixar o Aspire service discovery e `launchSettings.json`/`appsettings.json` cuidarem disso.
- Não contornar a camada de DTO para enviar entidades EF Core pela rede.
- Não implementar cálculos de meta/projeção de saldo divergentes das regras descritas em [Regras de Negócio](#regras-de-negócio-do-domínio-financeiro) sem antes atualizar esta seção.
- Não executar `delete_transaction` (ou qualquer operação destrutiva) sem confirmação explícita do chamador.

## Perguntas Abertas / TODO
- [x] Store de persistência: **PostgreSQL** (EF Core + Npgsql, recurso Aspire).
- [x] Provedor de auth: **JWT customizado** (emissão/validação próprias, sem Identity/Entra ID).
