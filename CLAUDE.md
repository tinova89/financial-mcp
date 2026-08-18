# CLAUDE.md

This file guides Claude Code when working in this repository. Keep it up to date as the project evolves.

## Project Overview

A financial **MCP Server** (Model Context Protocol), exposed via a **custom connector**, that lets Claude (via chat/Claude.ai, Claude Desktop, Claude Code, etc.) **query, insert, and modify** financial transactions for:

- **Checking Account (CC)** — bank statements
- **Credit Card (CD)** — card statements (Nubank, Bradesco, Sofisa, and future ones)

The MCP also exposes tools for **budget goals** (`Meta_Valor` per category/month), applying the business rules described in the [Business Rules](#financial-domain-business-rules) section below — these rules are the source of truth for any calculation logic implemented in the backend and **must** be kept in sync with the code's actual behavior.

Built with:
- **Backend:** .NET 10, orchestrated via .NET Aspire
- **Orchestration:** .NET Aspire AppHost for local development, service discovery, and observability
- **Persistence:** PostgreSQL (via EF Core + Npgsql), provisioned as an Aspire resource (`AddPostgres`/`AddDatabase`)
- **Auth:** custom JWT provider (its own issuance and validation in `FinancialMcp.Api`/`FinancialMcp.Application`, no Identity/Entra ID)
- **Protocol:** MCP (Model Context Protocol) over the same API host, authenticated via JWT bearer

> Related documentation (outside this file) can be freely referenced via relative link — see [Linking Other Artifacts and Docs](#linking-other-artifacts-and-docs).

## Solution Structure

```
/FinancialMcp.sln
/src
  /FinancialMcp.AppHost            -> Aspire orchestration project (entry point for `dotnet run`)
  /FinancialMcp.ServiceDefaults    -> Shared Aspire defaults (health checks, telemetry, resilience)
  /FinancialMcp.Api                -> ASP.NET Core Web API + MCP Server host (+ SignalR Hub, if needed for real-time updates)
  /FinancialMcp.Application        -> Application/business logic (use cases, services, MCP tools)
  /FinancialMcp.Domain              -> Domain entities, value objects (Transaction, Account, CreditCard, BudgetGoal)
  /FinancialMcp.Infrastructure      -> EF Core, persistence, CSV statement import/parsing, external integrations

/tests
  /FinancialMcp.Api.Tests
  /FinancialMcp.Application.Tests
```

## Common Commands

### Run everything (backend + Aspire dashboard)
```bash
dotnet run --project src/FinancialMcp.AppHost
```
The Aspire dashboard opens automatically (default: https://localhost:17090) showing logs, traces, and metrics for all services, including MCP tool calls.

### Run tests
```bash
dotnet test
```

### Build
```bash
dotnet build
```

### Add an EF Core migration (Postgres)
```bash
dotnet ef migrations add <Name> --project src/FinancialMcp.Infrastructure --startup-project src/FinancialMcp.Api
```
The Postgres connection string is injected by Aspire (`ConnectionStrings__financialmcp-db` variable); don't hardcode host/port/credentials in `appsettings.json` — only local development defaults, if needed.

## Architecture

### MCP
- **Auth:** MCP connections use the same JWT bearer scheme as the REST API (custom provider, see [Authentication](#authentication-custom-jwt)); token passed via `accessTokenFactory` on the client.
- **Exposed tools** — suggested names, adjust to match the actual implementation:
  - `list_transactions` — lists checking account and/or credit card transactions with filters (type, status, category/subcategory, account, card, period, reference month).
  - `get_transaction` — detail of a specific transaction.
  - `create_transaction` — inserts a new transaction (checking account or credit card), honoring the required fields for each statement type.
  - `update_transaction` — changes fields of an existing transaction (e.g. status, category, amount, date).
  - `delete_transaction` — removes a transaction; must be a soft delete. **Destructive operation**: require explicit confirmation from the caller before executing.
  - `reconcile_transaction` — marks a transaction as `Conciliado` (checking account) or the equivalent for credit card.
  - `list_categories` — lists parent categories and subcategories in use (parsed from `Categoria-mãe/Subcategoria`).
  - `get_budget_status` — calculates `Gasto_Real`, `Saldo_Meta`, and `% Utilizado` per category/month, per `metas_orcamento.csv` (see rules below).
  - `create_category_budget` — registers a budget goal (`Meta_Valor`) for a parent category and calendar month (`Monthly` or `OneTime`, see rules below); rejected if the category is a subcategory or a goal already exists for that category/month.
  - `import_statement` — imports a new CSV statement (checking account or credit card) into the database.
  - `list_accounts`/`get_account`/`create_account`/`update_account`/`delete_account` — CRUD for financial accounts (checking, investment, wallet, etc.); never returns/operates on credit cards (see below).
  - `list_credit_cards`/`get_credit_card`/`create_credit_card`/`update_credit_card`/`delete_credit_card` — CRUD for credit cards. A `CreditCard` is a kind of `Account` (EF Core Table-Per-Hierarchy, same `accounts` table) with its own `ClosingDay`/`DueDay`/`PaymentAccountId`; its `Kind` is always forced to `Credit` and is never a settable parameter.
- Every tool that **writes/modifies** data (create/update/delete/import) must validate fields according to the source format (`;` separator, `dd/mm/yyyy` dates, dot-decimal) before persisting.

### Aspire
- `FinancialMcp.AppHost/AppHost.cs` defines the resources: Postgres (`AddPostgres("financialmcp-postgres").AddDatabase("financialmcp-db")`), the API project (`.WithReference(postgres)`), Redis if needed for caching, a React app if present (as an `npm` resource).
- `FinancialMcp.ServiceDefaults` configures OpenTelemetry, health checks (`/health`, `/alive`), and default resilience handlers — reference it in every service project.
- Service discovery: reference other services by their Aspire resource name (e.g. `https+http://financialmcp-api`), never hardcoded URLs.
- In production, the AppHost's Postgres resource is replaced by the real connection string via environment configuration/secret — the local dashboard must never point at the production database.

### Persistence (Postgres)
- **Provider:** EF Core with `Npgsql.EntityFrameworkCore.PostgreSQL`, `DbContext` in `FinancialMcp.Infrastructure`.
- **Schema/Migrations:** all EF Core migrations are versioned under `FinancialMcp.Infrastructure/Migrations`; never edit a migration already applied to a shared environment — create a new one.
- **Types:** map `Valor`/`Meta_Valor`/`Gasto_Real`/`Saldo_Meta` as `numeric` (never `double precision`); dates (`Data prevista`, `Data efetiva`, `Data Conciliado`, `Venc. Fatura`) as `date`/`timestamptz` as appropriate, never `text`.
- **Indexes:** ensure an index on `(Mês_Ano, CategoriaMae)` and on `(Status, Tipo)` to speed up `get_budget_status` aggregations.
- **Soft delete:** `delete_transaction` sets a `DeletedAt`/`IsDeleted` column; a global query filter on the `DbContext` excludes deleted records from all queries by default (explicit administrative queries can bypass the filter via `IgnoreQueryFilters()`).
- **Connection string:** always via Aspire service discovery/`ConnectionStrings`, never hardcoded (see [Common Commands](#common-commands)).

### Authentication (Custom JWT)
- **Issuance:** a dedicated endpoint (e.g. `POST /auth/token`) in `FinancialMcp.Api` validates credentials and issues a signed JWT (HMAC or RSA, key via Aspire configuration/secret) with minimal claims (`sub`, `exp`, `iat`, and scope/role claims needed to distinguish read vs. write operations in the MCP tools).
- **Validation:** standard ASP.NET Core middleware (`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`), configured to validate issuer, audience, lifetime, and signature; the same validation configuration is reused by the MCP host (same auth pipeline as the REST API, not a parallel scheme).
- **Refresh:** if needed, an opaque refresh token persisted in Postgres (its own table, never reusing the users table directly), with a short access-token expiration and refresh-token rotation on each use.
- **Secrets:** the signing key and other sensitive parameters never live in a versioned `appsettings.json` — use `dotnet user-secrets` locally and an environment/secret manager configuration in production.
- **Usage scope:** this is the project's only auth mechanism (no ASP.NET Core Identity, no Entra ID); any future need for federated login should be handled as an extension of this provider, not a silent replacement.

## Financial Domain Business Rules

These rules **govern** the behavior of the query and calculation MCP tools (`get_budget_status`, `list_transactions`, etc.). Any behavior change in the code must keep this document up to date.

### Category and subcategory
- The `Categoria` column in the statements (CC and CD) is treated as `Categoria-mãe/Subcategoria`, split on the `/` character when present.
- A `BudgetGoal` always references a parent `TransactionCategory` (`RawCategoryId`), never a subcategory — aggregation for a goal (e.g. `Moradia`) sums **all** rows whose parent category is `Moradia`, regardless of subcategory.
- Also generate a breakdown by subcategory (secondary, doesn't create its own goal).

### Budget goals (`get_budget_status`)
1. **Status filter** (default, unless stated otherwise): only `Status = Conciliado`. Ignore `Agendado` (checking account) and `Nconciliado` (credit card).
2. **Type filter**: only `Tipo = Despesa`. Don't include `Receita` by default. Never include `Transferência` or `Pagamento` (the "Pagamento de cartão" entry in the checking account is excluded so as not to double-count spending already counted via the card's entries).
3. **Month reference date (`Mês_Ano`)**:
   - Checking account (when `Conciliado`): use the **"Data Conciliado"** column.
   - Credit card: use the **"Venc. Fatura"** column (not "Data efetiva") — reflects the month in which the amount actually impacts the payment account, honoring the rule of rolling to the next business day when the due date falls on a weekend.
4. **Goal period (`BudgetGoal.Period`)**: which registered goal row is "in effect" for a given category/month, per `BudgetGoal.ResolveEffective`:
   - `OneTime` — matches only its own `PeriodReference` (Year/Month); no automatic repetition.
   - `Monthly` — applies from its own `PeriodReference` onward, until a later `Monthly` row for the same category (a later `PeriodReference`) supersedes it.
   - A `OneTime` row wins over a `Monthly` one for the exact month it targets.
5. **Formulas**:
   - `Gasto_Real` (default, unless stated otherwise) = sum of the absolute value of reconciled expenses in the same parent category in the same `Mês_Ano` (CC + CD combined).
   - `Saldo_Meta` = `Meta_Valor` − `Gasto_Real`.
   - `% Utilizado` = `Gasto_Real` / `Meta_Valor`.
6. Categories without a goal in effect for the requested month don't appear in the budget goal sheet/query (but can appear in a separate report if requested via `list_transactions`).

## Code Conventions

- **C#:** Nullable reference types enabled, file-scoped namespaces, primary constructors when they improve clarity.
- **Async:** all I/O-bound methods are `async`/`await`; `Async` suffix.
- **DTOs:** never expose domain entities directly via MCP/SignalR/REST — map to DTOs/records.
- **Money:** use `decimal` (never `double`/`float`) for `Valor`, `Meta_Valor`, `Gasto_Real`, `Saldo_Meta`.
- **Dates:** treat `Data prevista`, `Data efetiva`, `Data Conciliado`, and `Venc. Fatura` as explicit date types (not string); centralize the "next business day" logic in a single helper, reused by `get_budget_status`.
- **Category/Subcategory:** centralize the `Categoria-mãe/Subcategoria` parsing in a single helper/value object, reused by every MCP tool that aggregates by category.
- **Validation:** FluentValidation for request/command validation in `FinancialMcp.Application`, applied via a MediatR pipeline behavior (see below), including statement format validation on import (`import_statement`).
- **Naming:** methods in PascalCase on the C# side, camelCase on the client side (`ReceiveMessage` ↔ `receiveMessage`).

### Mediator Pattern (MediatR)

All MCP tools and REST endpoints must be **thin**: they only build the `IRequest`/`IRequest<TResponse>` and call `IMediator.Send(...)` (or `Publish` for notifications). No business rule should live in the MCP tool/handler or in the controller — the logic belongs to the MediatR handlers in `FinancialMcp.Application`.

- **Explicit CQRS:** always separate into **Commands** (writes: `create_transaction`, `update_transaction`, `delete_transaction`, `reconcile_transaction`, `import_statement`) and **Queries** (reads: `list_transactions`, `get_transaction`, `list_categories`, `get_budget_status`).
- **Feature-based organization:** group each request + handler + validator (+ response DTO) in the same feature folder, not in loose generic `Commands/`, `Queries/`, `Handlers/` folders:
  ```
  FinancialMcp.Application/
    Transactions/
      CreateTransaction/
        CreateTransactionCommand.cs        (record, implements IRequest<TransactionDto>)
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
- **Naming:** `<Action><Entity>Command`/`Query` for the request, `<Name>Handler` for the handler, `<Name>Validator` for the FluentValidation validator. Requests are immutable `record`s; never reuse domain entities as a request.
- **One handler per request:** each `IRequestHandler<TRequest, TResponse>` must be the single unit orchestrating repositories/services for that use case. Pure calculation rules (installments, billing cycle, next business day, category aggregation) live in domain/application services injected into the handler — not written inline in the handler — to allow isolated unit testing (see [Testing Guidelines](#testing-guidelines)).
- **Pipeline behaviors** (registered once in `FinancialMcp.Application`, via `AddMediatR` + `AddTransient(typeof(IPipelineBehavior<,>), ...)`), in order:
  1. `LoggingBehavior<TRequest,TResponse>` — logs request/response (no sensitive data) and integrates with Aspire/OpenTelemetry tracing.
  2. `ValidationBehavior<TRequest,TResponse>` — runs every `IValidator<TRequest>` (FluentValidation) before the handler; throws a custom `ValidationException` on failure, mapped to the appropriate MCP/HTTP error.
  3. `TransactionBehavior<TRequest,TResponse>` (only for Commands that write via EF Core) — opens a database transaction, runs the handler, commits/rolls back.
- **Notifications (`INotification`)** for side effects decoupled from the main flow, without coupling the write handler to other modules' logic:
  - E.g.: `TransactionReconciledNotification`, published by `ReconcileTransactionCommandHandler`, consumed by an `INotificationHandler` that recalculates cached `get_budget_status` or notifies clients via SignalR.
  - Never use `Publish` for logic that needs a synchronous return value or that is a mandatory part of the business rule — that remains the responsibility of the main `Command`/`Handler`.
- **Destructive operations:** `DeleteTransactionCommand` must carry an explicit confirmation field (e.g. `Confirm: bool`) validated by `ValidationBehavior`; the handler rejects execution if `Confirm != true`, reinforcing the rule of "never executing without explicit confirmation" (see [What Claude Should Avoid](#what-claude-should-avoid)).
- **Registration:** `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FinancialMcp.Application.AssemblyMarker).Assembly))` centralized in `FinancialMcp.Application`, referenced from `FinancialMcp.Api` — never register MediatR assemblies directly in the API layer.

## Testing Guidelines

- Unit test business rules in isolation from the handlers/MCP tools (extract them into services; tools/handlers should stay thin).
- Specifically cover with tests:
  - `Gasto_Real` aggregation by parent category vs. full subcategory.
  - Exclusion of `Transferência`, `Pagamento`, and `Receita` from the budget goal calculation.
- Use `TestServer` + a real MCP client (or equivalent) for integration tests of the exposed tools.
- Frontend (if any): React Testing Library for components, mocking the MCP/SignalR connection instead of opening real sockets.

## Linking Other Artifacts and Docs

This `CLAUDE.md` is the entry point, but it doesn't need to hold everything — linking other artifacts (architecture docs, ADRs, diagrams, API specs, other subfolder `CLAUDE.md` files) is allowed and encouraged, without breaking parsing or navigation:

- **Repo-relative links:** always use a path relative to the root or the current folder (e.g. `docs/adr/0001-postgres.md`, `../FinancialMcp.Infrastructure/README.md`), never a local machine's absolute path.
- **Claude Code import (`@path`):** for content that should be automatically loaded as context (not just as a clickable link), use the `@docs/file.md` syntax instead of rewriting the content here. Avoid circular imports (A imports B which imports A) and avoid importing very large files unnecessarily — prefer a summary here + a link/import to the detail.
- **Nested `CLAUDE.md` files:** subfolders (e.g. `src/FinancialMcp.Infrastructure/CLAUDE.md`) may exist with rules specific to that module; this root file doesn't need to duplicate them, just point to them when relevant.
- **Stable anchors:** when linking to a specific section of this file (anchor like `#authentication-custom-jwt`), always prefer the title exactly as written, so the link doesn't break if sections get reordered.
- **Artifacts external to the repo** (Notion, Confluence, Aspire dashboards, Grafana, etc.): link normally with standard markdown; there's no need to validate the availability of these links during build/CI.
- A broken link or an artifact not yet created must **not** block Claude from reading/using this `CLAUDE.md` — treat it as an optional reference, not a mandatory dependency.

## What Claude Should Avoid

- Don't hardcode ports/URLs — let Aspire service discovery and `launchSettings.json`/`appsettings.json` handle that.
- Don't bypass the DTO layer to send EF Core entities over the network.
- Don't implement budget goal calculations that diverge from the rules described in [Business Rules](#financial-domain-business-rules) without first updating that section.
- Don't run `delete_transaction` (or any destructive operation) without explicit confirmation from the caller.

## Open Questions / TODO
- [x] Persistence store: **PostgreSQL** (EF Core + Npgsql, Aspire resource).
- [x] Auth provider: **Custom JWT** (its own issuance/validation, no Identity/Entra ID).
