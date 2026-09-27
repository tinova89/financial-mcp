# CLAUDE.md

Financial **MCP Server** (.NET 10 + Aspire, PostgreSQL/EF Core, custom JWT) that lets Claude query, insert and modify transactions for **Checking Accounts (CC)** and **Credit Cards (CD)**, plus monthly **budget goals** per parent category.

This project follows **Spec-Driven Development (SDD)**: the specs in [`/specs`](specs/) are the source of truth; code is derived from them.

## Always load

@.specify/memory/constitution.md

## Where things live

| Need | Read |
|---|---|
| Non-negotiable rules (money, dates, soft delete, DTOs, CQRS, `X-Account-Group`) | [`.specify/memory/constitution.md`](.specify/memory/constitution.md) |
| Budget formulas, status rules, category rules | [`specs/domain/business-rules.md`](specs/domain/business-rules.md) |
| Domain terms and enum values | [`specs/domain/glossary.md`](specs/domain/glossary.md) |
| MCP tools (params, behavior, scopes) and REST endpoints | [`specs/domain/mcp-tools.md`](specs/domain/mcp-tools.md) |
| Aspire, persistence, auth, account group | [`specs/architecture.md`](specs/architecture.md) |
| A feature (what / how / steps) | `specs/<NNN-FT-name>/spec.md` · `plan.md` · `tasks.md` |
| What's next | [`specs/backlog.md`](specs/backlog.md) |

## Commands

```bash
dotnet run --project src/FinancialMcp.AppHost   # backend + Aspire dashboard
dotnet build
dotnet test
dotnet tool restore                              # pins dotnet-ef to EF Core 10
dotnet ef migrations add <Name> --project src/FinancialMcp.Infrastructure.Persistence --startup-project src/FinancialMcp.Api
```

## Solution layout

```
src/
  FinancialMcp.AppHost          Aspire orchestration (entry point)
  FinancialMcp.ServiceDefaults  health checks, telemetry, resilience
  FinancialMcp.Api              REST + MCP host (tools in Mcp/Tools)
  FinancialMcp.Application      MediatR handlers, validators, services (one folder per feature)
  FinancialMcp.Domain           entities, value objects, enums
  FinancialMcp.Infrastructure   EF Core, CSV import, integrations
tests/
  FinancialMcp.Api.Tests
  FinancialMcp.Application.Tests
```

## SDD workflow (per feature)

1. **Specify** → `spec.md` (what/why, user stories, acceptance criteria, no class names).
2. **Clarify** → resolve the "Open questions" in `spec.md` before planning.
3. **Plan** → `plan.md` (migrations, handlers, validators, contracts).
4. **Tasks** → `tasks.md` (small, ordered, checkable steps).
5. **Implement** → read `spec.md` + `plan.md` + constitution, then work through `tasks.md`.

Templates: [`specs/_templates/`](specs/_templates/).

## Before finishing any feature

- Every acceptance criterion in `spec.md` is checked and every task in `tasks.md` is done.
- `dotnet build` and `dotnet test` pass.
- If behavior changed, update the spec docs **first** (`business-rules.md`, `glossary.md`, `mcp-tools.md`) — never leave them behind the code.
- Set the feature `Status:` to `Implemented` and update `specs/backlog.md`.
- Don't add feature-specific detail here — it belongs in the feature folder.
