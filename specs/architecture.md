# Architecture

## Aspire

- `AppHost.cs`: `AddPostgres("financialmcp-postgres").AddDatabase("financialmcp-db")`, and the API project uses `.WithReference(postgres)`. Add Redis or React only when we need them.
- `ServiceDefaults`: OpenTelemetry, `/health`, `/alive`, resilience. Every service uses it.
- Services find each other by resource name (`https+http://financialmcp-api`).
- In production, we change the Postgres resource for a real connection string from secrets. The local dashboard never connects to production.

## Persistence

- EF Core + Npgsql. The `DbContext` is in Infrastructure. Migrations are in `Infrastructure/Migrations`.
- The connection string comes in as `ConnectionStrings__financialmcp-db`.
- Indexes: reference month/year + parent category; `(Status, Type)`; `accounts.Group`.
- Soft delete uses a global query filter. Admin queries can use `IgnoreQueryFilters()`.
- The status timestamps on `transactions` (`SubmittedForReviewAt`/`ScheduledAt`/`ConfirmedAt`) are `timestamptz` and can be null.
- `transaction_revisions`: it has the same columns as `transactions`, but without `UpdatedAt`, the soft-delete columns and the status timestamps. Its `CreatedAt` is the time it was sent for review. It follows its parent with `HasQueryFilter(r => !r.Transaction.IsDeleted)`. Config: `TransactionRevisionConfiguration`.
- `accounts`: TPH for `Account`/`CreditCard` (one table for both).

## Authentication (custom JWT)

- `POST /auth/token` and `/auth/refresh` create signed JWTs (`sub`, `exp`, `iat`, `scope`). The key comes from user-secrets or a secret manager.
- Scopes (`AuthScopes.Default`): `transactions:read`, `transactions:write`, `budget:read`, `approval`.
- `approval` is only for `approve_revision`. The tool checks it inside its own code (it throws `ForbiddenException` → 403), because MCP tools have no endpoint metadata. `AuthScopes.ApprovalPolicy` (`"ApprovalScope"`) is registered for the day we turn the global JWT middleware on again.
- MCP uses the same JWT bearer pipeline as REST. The client sends the token with `accessTokenFactory`.
- Refresh tokens (if we use them): opaque, in their own Postgres table, and changed for a new one each time they are used.
- Open question: who gets `approval`.

## Account group (`X-Account-Group`)

- `RequireGroupHeaderMiddleware` (`Api/Common`) runs right after `ExceptionHandlingMiddleware`. If the header is missing or empty → 400 `problem+json`.
- `ICurrentGroupService.Group` (Application) is implemented by `CurrentGroupService` (Infrastructure, `IHttpContextAccessor`).
- The create handlers for accounts and cards take `Group` from it.
- Separating read data by group is not built yet (see [backlog](backlog.md)).

## Linking docs

- Use only links relative to the repo. Use `@path` imports in `CLAUDE.md` only for small files that we always need (today: the constitution).
- Subprojects can have their own `CLAUDE.md` with rules for that module.
- A broken or missing link never stops work.
