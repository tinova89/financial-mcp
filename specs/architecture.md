# Architecture

## Aspire

- `AppHost.cs`: `AddPostgres("financialmcp-postgres").AddDatabase("financialmcp-db")`, API project `.WithReference(postgres)`; Redis/React only if needed.
- `ServiceDefaults`: OpenTelemetry, `/health`, `/alive`, resilience. Referenced by every service.
- Service discovery by resource name (`https+http://financialmcp-api`).
- Production swaps the Postgres resource for a real connection string via secrets; local dashboard never points at production.

## Persistence

- EF Core + Npgsql, `DbContext` in Infrastructure; migrations under `Infrastructure/Migrations`.
- Connection string injected as `ConnectionStrings__financialmcp-db`.
- Indexes: reference month/year + parent category; `(Status, Type)`; `accounts.Group`.
- Soft delete via global query filter; admin queries may use `IgnoreQueryFilters()`.
- `transactions` status timestamps `SubmittedForReviewAt`/`ScheduledAt`/`ConfirmedAt`: nullable `timestamptz`.
- `transaction_revisions`: same shape as `transactions` minus `UpdatedAt`, soft-delete columns and status stamps. Its `CreatedAt` is the submission timestamp. Follows parent via `HasQueryFilter(r => !r.Transaction.IsDeleted)`. Config: `TransactionRevisionConfiguration`.
- `accounts`: TPH for `Account`/`CreditCard`.

## Authentication (custom JWT)

- `POST /auth/token` and `/auth/refresh` issue signed JWTs (`sub`, `exp`, `iat`, `scope`). Key via user-secrets / secret manager.
- Scopes (`AuthScopes.Default`): `transactions:read`, `transactions:write`, `budget:read`, `approval`.
- `approval` gates only `approve_revision`, checked inline in the tool (throws `ForbiddenException` → 403) because MCP tools have no endpoint metadata. `AuthScopes.ApprovalPolicy` (`"ApprovalScope"`) is registered for when global JWT middleware is re-enabled.
- MCP reuses the REST JWT bearer pipeline; client sends token via `accessTokenFactory`.
- Refresh tokens (if used): opaque, own Postgres table, rotated on use.
- Open: who is granted `approval`.

## Account group (`X-Account-Group`)

- `RequireGroupHeaderMiddleware` (`Api/Common`), right after `ExceptionHandlingMiddleware`; missing/blank → 400 `problem+json`.
- `ICurrentGroupService.Group` (Application) implemented by `CurrentGroupService` (Infrastructure, `IHttpContextAccessor`).
- Create handlers for accounts/cards stamp `Group` from it.
- Read isolation not built yet (see [backlog](backlog.md)).

## Linking docs

- Repo-relative links only. Use `@path` imports in `CLAUDE.md` only for small, always-needed files (today: constitution).
- Nested `CLAUDE.md` in subprojects allowed for module-specific rules.
- Broken/missing links never block work.
