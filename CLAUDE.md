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
- **Exposed tools** — 15 tools total, all `[McpServerToolType]` classes under `FinancialMcp.Api/Mcp/Tools`, registered by reflection (`WithToolsFromAssembly()` in `Program.cs`). Each is "thin" — see [Mediator Pattern](#mediator-pattern-mediatr):
  - **`CategoryTools`**
    - `list_categories` — lists every distinct parent category currently in use, each with its distinct subcategories (parsed from `Categoria-mãe/Subcategoria`, split on `/`). Read-only, no parameters. Purely informational — doesn't imply a budget goal exists (see `get_budget_status`).
    - `lookup_category` — resolves a category for a transaction `description`, from the description→category mapping table learned automatically every time `create_transaction`/`update_transaction` categorizes a transaction (most recent categorization wins). Useful before `create_transaction`/`update_transaction` to reuse a past categorization instead of guessing.
  - **`TransactionTools`**
    - `list_transactions` — lists transactions with filters: `periodStart`/`periodEnd` (required, inclusive `ExpectedDate` range), `type`/`status` (optional enums, see below), `category`/`subcategory`, `accountId` (checking account or credit card), `year`/`month` (reference month, see [Budget goals](#budget-goals-get_budget_status)), `page`/`pageSize`. Read-only, paginated, ordered by `expectedDate` descending.
    - `get_transaction` — full detail of a single transaction by `transactionId`. Read-only.
    - `create_transaction` — inserts a new transaction (checking account or credit card), honoring the required fields for each statement type (`invoiceDueDate` required for credit-card `accountId`s, installment fields required when `recurrence = Installment`). Non-destructive.
    - `update_transaction` — partial patch of an existing transaction (`status`, `rawCategory`, `amount`, dates); fields left `null` keep their stored value. `accountId`, `recurrence`, and installment fields aren't patchable here. Non-destructive.
    - `delete_transaction` — soft delete (`IsDeleted`/`DeletedAt`). **Destructive operation**: requires an explicit `confirm = true` argument, rejected by validation otherwise — always confirm with the caller first.
    - `reconcile_transaction` — marks a transaction `Reconciled`; sets `reconciledDate` only for checking-account transactions (credit-card rows key off `invoiceDueDate` instead). Publishes `TransactionReconciledNotification` afterwards. Non-destructive.
  - **`CheckingAccountTools`** (formerly `AccountTools`/`list_accounts`/`get_account`) — **read-only** for financial accounts (checking, investment, wallet, etc.); never returns/operates on credit cards.
    - `list_checking_accounts` — every registered non-credit-card account, ordered by `displayName`, each including the `creditCardIds` whose bill is paid from it.
    - `get_checking_account` — detail of a single account by `accountId`.
    - Create/update/delete are **not** MCP tools — they're REST-only (see below).
  - **`CreditCardTools`** — **read-only** for credit cards. A `CreditCard` is a kind of `Account` (EF Core Table-Per-Hierarchy, same `accounts` table) with its own `ClosingDay`/`DueDay`/`PaymentAccountId`; its `Kind` is always forced to `Credit` and is never a settable parameter.
    - `list_credit_cards` — every registered credit card, ordered by `displayName`.
    - `get_credit_card` — detail of a single credit card by `creditCardId`.
    - Create/update/delete are also REST-only.
  - **`BudgetTools`**
    - `get_budget_status` — calculates `Gasto_Real`/`Saldo_Meta`/`% Utilizado` per category for a given `year`/`month` (both required), for every category with a budget goal in effect that month, per the [Budget goals](#budget-goals-get_budget_status) rules below. Read-only.
    - `create_category_budget` — registers a budget goal (`Meta_Valor`) for a parent category (`categoryId`) and calendar month (`year`/`month`), with an `amount`/`currencyCode` and a `period` (`Monthly` or `OneTime`, see rules below). Rejected if `categoryId` is a subcategory or a goal already exists for that category/month. Non-destructive.
  - **`StatementTools`**
    - `import_statement` — imports a CSV statement (`csvContent`) into the account identified by `accountId`; whether each row parses as a checking-account or credit-card statement is inferred from that account's `Account.Kind`, never a caller-supplied flag. Invalid lines are skipped with a warning rather than aborting the whole import; the imported batch commits atomically. Non-destructive.
- **Enum parameters are sent as plain integers, not strings.** No `JsonStringEnumConverter` is registered, so C#-enum-typed MCP parameters — `create_transaction`'s `type`/`status`/`recurrence`, `update_transaction`'s `status`, `list_transactions`' `type`/`status`, and `create_category_budget`'s `period` — serialize in the tool schema as `int`, not the enum member's name. Each tool's `Description` documents the mapping inline (e.g. `0 - Expense`, `1 - Income`, ...); consult the tool description rather than assuming the string name is accepted. `TransactionStatus`/`TransactionType`/`RecurrenceType` (`FinancialMcp.Domain.Enums`) start at `0`; `BudgetPeriodType` is `1 - Monthly`, `2 - OneTime`.
- Every tool that **writes/modifies** data (create/update/delete/import) must validate fields according to the source format (`;` separator, `dd/mm/yyyy` dates, dot-decimal) before persisting.
- **Checking Accounts / Credit Cards REST API:** create/update/delete for both entities live outside MCP, as minimal-API endpoints mapped in `FinancialMcp.Api/Program.cs` (`#region Checking Accounts API` / `#region Credit Cards API`) — `POST`/`PUT`/`DELETE /api/financial/checking-accounts` (`CreateCheckingAccount`/`UpdateCheckingAccount`/`DeleteCheckingAccount`) and `POST`/`PUT`/`DELETE /api/financial/credit-cards` (`CreateCreditCard`/`UpdateCreditCard`/`DeleteCreditCard`), each dispatching the same `CreateCheckingAccountCommand`/`CreateCreditCardCommand`/etc. through `IMediator` as any MCP tool would. Same request/response contract either way — only the transport differs.

### Aspire
- `FinancialMcp.AppHost/AppHost.cs` defines the resources: Postgres (`AddPostgres("financialmcp-postgres").AddDatabase("financialmcp-db")`), the API project (`.WithReference(postgres)`), Redis if needed for caching, a React app if present (as an `npm` resource).
- `FinancialMcp.ServiceDefaults` configures OpenTelemetry, health checks (`/health`, `/alive`), and default resilience handlers — reference it in every service project.
- Service discovery: reference other services by their Aspire resource name (e.g. `https+http://financialmcp-api`), never hardcoded URLs.
- In production, the AppHost's Postgres resource is replaced by the real connection string via environment configuration/secret — the local dashboard must never point at the production database.

### Persistence (Postgres)
- **Provider:** EF Core with `Npgsql.EntityFrameworkCore.PostgreSQL`, `DbContext` in `FinancialMcp.Infrastructure`.
- **Schema/Migrations:** all EF Core migrations are versioned under `FinancialMcp.Infrastructure/Migrations`; never edit a migration already applied to a shared environment — create a new one.
- **Types:** map `Valor`/`Meta_Valor`/`Gasto_Real`/`Saldo_Meta` as `numeric` (never `double precision`); dates (`Data prevista`, `Data efetiva`, `Data Conciliado`, `Venc. Fatura`) as `date`/`timestamptz` as appropriate, never `text`.
- **Indexes:** ensure an index on `(Mês_Ano, CategoriaMae)` and on `(Status, Tipo)` to speed up `get_budget_status` aggregations; `accounts.Group` is indexed too (see [Account Group](#account-group-x-account-group-header)).
- **Soft delete:** `delete_transaction` sets a `DeletedAt`/`IsDeleted` column; a global query filter on the `DbContext` excludes deleted records from all queries by default (explicit administrative queries can bypass the filter via `IgnoreQueryFilters()`).
- **Connection string:** always via Aspire service discovery/`ConnectionStrings`, never hardcoded (see [Common Commands](#common-commands)).

### Authentication (Custom JWT)
- **Issuance:** a dedicated endpoint (e.g. `POST /auth/token`) in `FinancialMcp.Api` validates credentials and issues a signed JWT (HMAC or RSA, key via Aspire configuration/secret) with minimal claims (`sub`, `exp`, `iat`, and scope/role claims needed to distinguish read vs. write operations in the MCP tools).
- **Validation:** standard ASP.NET Core middleware (`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`), configured to validate issuer, audience, lifetime, and signature; the same validation configuration is reused by the MCP host (same auth pipeline as the REST API, not a parallel scheme).
- **Refresh:** if needed, an opaque refresh token persisted in Postgres (its own table, never reusing the users table directly), with a short access-token expiration and refresh-token rotation on each use.
- **Secrets:** the signing key and other sensitive parameters never live in a versioned `appsettings.json` — use `dotnet user-secrets` locally and an environment/secret manager configuration in production.
- **Usage scope:** this is the project's only auth mechanism (no ASP.NET Core Identity, no Entra ID); any future need for federated login should be handled as an extension of this provider, not a silent replacement.

### Account Group (`X-Account-Group` header)
- **Purpose:** `Account.Group` (inherited by `CreditCard` — same `accounts` table) separates accounts into independent groups (e.g. `"HOME"`, `"SOM"`) — a free-text string, no fixed/enumerated list of valid values.
- **Header requirement:** every request in the system — every REST endpoint under `/api/financial/*` and the MCP endpoint (`/mcp`) alike — must include the `X-Account-Group` HTTP header. Enforced by `RequireGroupHeaderMiddleware` (`FinancialMcp.Api/Common`), registered right after `ExceptionHandlingMiddleware` so it runs before any endpoint; a missing/blank header gets a `400` `application/problem+json` response before any handler executes. Exempt: `/health`, `/alive`, `/openapi`, `/scalar` (infra/docs routes, not scoped to a group).
- **Access:** `ICurrentGroupService.Group` (`FinancialMcp.Application.Common.Interfaces`, implemented by `CurrentGroupService` in `FinancialMcp.Infrastructure` via `IHttpContextAccessor`) exposes the current request's group value to handlers. `CurrentGroupService.GroupHeaderName` is the single source of truth for the header name string — the middleware reads the same constant, so they can't drift apart.
- **On create:** `CreateCheckingAccountCommandHandler`/`CreateCreditCardCommandHandler` stamp `Group` from `ICurrentGroupService.Group` — it is **never** a caller-supplied command field, exactly like `Kind` (see [Solution Structure](#solution-structure) note on `Account.Kind`).
- **Current scope (important):** only header enforcement + stamping on create are implemented so far. Existing read tools (`list_checking_accounts`, `get_checking_account`, `list_credit_cards`, `get_credit_card`, `list_transactions`, `get_transaction`, `get_budget_status`, `list_categories`, `lookup_category`, etc. — see [Exposed tools](#mcp)) do **not** yet filter by the requesting group — two different `X-Account-Group` values currently see the same data on reads. Full data isolation (a global EF Core query filter scoping every Account/CreditCard/Transaction query to the current group, mirroring the existing soft-delete filter) is a deliberate follow-up, not yet built — don't assume group-based read isolation exists until this line is updated.

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

- **Explicit CQRS:** always separate into **Commands** (writes: `create_transaction`, `update_transaction`, `delete_transaction`, `reconcile_transaction`, `import_statement`, `create_category_budget`, plus the REST-only account/credit-card create/update/delete) and **Queries** (reads: `list_transactions`, `get_transaction`, `list_categories`, `lookup_category`, `get_budget_status`, `list_checking_accounts`, `get_checking_account`, `list_credit_cards`, `get_credit_card`).
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
