<!--
Sync Impact Report
==================
Version change: (unversioned template) → 1.0.0
Bump rationale: first ratified constitution; every placeholder replaced with concrete governance.

Modified principles (template slot → new title):
  [PRINCIPLE_1_NAME] → I. Spec Is the Source of Truth
  [PRINCIPLE_2_NAME] → II. Thin Entry Points, Isolated Rules
  [PRINCIPLE_3_NAME] → III. Every Behavior Is Tested (NON-NEGOTIABLE)
  [PRINCIPLE_4_NAME] → IV. Test Fidelity Matches the Risk
  [PRINCIPLE_5_NAME] → V. Financial Correctness by Construction
  (added)            → VI. Maintainability and Simplicity

Added sections:
  - Quality Gates (was [SECTION_2_NAME])
  - Development Workflow (was [SECTION_3_NAME])

Removed sections: none

Templates requiring updates:
  ✅ .specify/templates/* — not modified (read the constitution at runtime; out of scope here)

Follow-up TODOs:
  - ✅ CLAUDE.md "Always load" import retargeted to @.specify/memory/constitution.md.
  - ✅ CLAUDE.md links to specs/domain/* (business-rules, glossary, mcp-tools),
    specs/architecture.md and specs/backlog.md now resolve — all files exist.
  - Quality Gate "dotnet test is green" conflicts with the known pre-existing HealthCheck test
    failure that needs a running Postgres; that test must be tagged/isolated as infrastructure-
    dependent (see Principle IV) so the default run can be green.
-->

# FinancialMcp Constitution

## Core Principles

### I. Spec Is the Source of Truth

- Behavior is defined in the specs (`specs/`, feature `spec.md`/`plan.md`/`tasks.md`) and the
  domain rule documents; code is derived from them, never the other way around.
- Any change that alters observable behavior (MCP tool contract, REST contract, business rule,
  calculation, enum value, validation rule) MUST update the governing spec document **in the same
  change**, before or together with the code. A spec that lags the code is a defect.
- Known deviations between documentation and code (e.g. a parser quirk locked in by a test) MUST
  be written down explicitly where the rule is documented, not left implicit.

Rationale: Claude and humans both navigate this system through its specs; stale specs cause wrong
calculations and wrong tool usage downstream.

### II. Thin Entry Points, Isolated Rules

- MCP tools and REST endpoints MUST only build a MediatR request and call `Send`/`Publish`. No
  business rule, query logic or validation lives in a tool, endpoint or controller.
- Every use case is one request + one handler (+ validator) in its own feature folder, split into
  Commands (writes) and Queries (reads).
- Pure calculation rules (budget aggregation, reference month, next business day, category
  parsing, installments) MUST live in injectable domain/application services or value objects so
  they can be unit-tested without a handler, a database or a transport.
- Cross-cutting concerns (logging, validation, database transactions) MUST be implemented once as
  pipeline behaviors, not repeated per handler.

Rationale: isolating rules from transport and persistence is what makes them cheap to test and
safe to change.

### III. Every Behavior Is Tested (NON-NEGOTIABLE)

- A feature is not done until each acceptance criterion in its `spec.md` is covered by at least
  one automated test that fails if the criterion is violated.
- Every handler, validator and domain service MUST have tests for: the success path, each
  validation/guard rejection, the not-found path where applicable, and every documented edge case.
- Every bug fix MUST add a regression test that fails before the fix and passes after it.
- Known, deliberately unfixed quirks MUST be locked in by a test that documents the current
  behavior and references where it is tracked, so any change to it is a conscious decision.
- Tests use the project's standard stack — xUnit, FluentAssertions, NSubstitute — and follow
  Arrange/Act/Assert with names that state the scenario and expected outcome.
- Tests MUST be deterministic: no dependence on wall-clock "today", execution order, shared
  mutable state or external network. Time is injected (e.g. `TimeProvider`/clock abstraction).

Rationale: this system moves money-shaped data on behalf of an AI agent; untested behavior is
unverified behavior.

### IV. Test Fidelity Matches the Risk

- Unit tests (mocks/substitutes) are the default for pure rules and handler orchestration.
- SQLite `:memory:` is used when a test needs real transactions or real EF Core query filters
  (soft delete, TPH scoping). EF Core's `InMemory` provider MUST NOT be used for these.
- A real-PostgreSQL integration test (`Testcontainers.PostgreSql`) is REQUIRED — a green SQLite
  test is not sufficient evidence — whenever code:
  - pushes an aggregation of money values into SQL (`SumAsync`, translated `GroupBy`, etc.)
    instead of materializing rows and summing in C# `decimal`;
  - depends on concurrent writes, transaction isolation, locking or Npgsql retry behavior;
  - depends on PostgreSQL-specific types, functions, collations or migration SQL.
- Exposed MCP tools SHOULD have at least one end-to-end test through `TestServer` and a real MCP
  client covering auth, the `X-Account-Group` header and the tool contract.
- Tests that need external infrastructure (Docker, Postgres) MUST be tagged as such so they can be
  run or excluded explicitly and never cause spurious failures in the default fast suite.

Rationale: SQLite and PostgreSQL differ in numeric aggregation and concurrency semantics; the
cheapest test that can actually detect the risk is the one that MUST be written.

### V. Financial Correctness by Construction

- Money is always `decimal` in C# and `numeric` in PostgreSQL; `double`/`float` are forbidden for
  amounts at every layer, including tests and intermediate calculations.
- Dates are explicit date/time types (`DateOnly`/`DateTimeOffset` → `date`/`timestamptz`), never
  strings. Each shared rule (next business day, `ParentCategory/Subcategory` parsing, `ActualSpend`)
  has exactly one implementation reused everywhere.
- Every write MUST be validated (FluentValidation, via the pipeline) before persistence; imports
  MUST validate the source format and report skipped lines rather than fail silently.
- Destructive operations require an explicit `confirm = true` enforced by validation, and use soft
  delete behind a global query filter.
- Entities never leave the application layer; transports only see DTOs/records.

Rationale: silent precision loss, date drift or unvalidated writes produce wrong budgets that no
one notices until reconciliation.

### VI. Maintainability and Simplicity

- Nullable reference types enabled; file-scoped namespaces; `async`/`await` end-to-end for I/O with
  the `Async` suffix; immutable `record` requests.
- Consistent naming: `<Action><Entity>Command`/`Query`, `<Name>Handler`, `<Name>Validator`;
  feature folders, not generic `Commands/`/`Handlers/` folders.
- A single source of truth for shared constants (header names, scopes, max lengths, enum
  mappings); no duplicated literals across layers.
- Build with zero new compiler warnings; do not suppress warnings or analyzers without a comment
  justifying it.
- YAGNI: no speculative abstractions, options or layers without a spec requirement. Added
  complexity MUST be justified in the feature's `plan.md`.
- Dead code, commented-out code and stale TODOs are removed or tracked in `specs/backlog.md`, not
  left in place. Temporary deviations (e.g. disabled auth middleware) MUST be documented where a
  reader of that code will find them.
- Hardcoded ports, URLs, connection strings and secrets are forbidden; use Aspire service
  discovery, configuration and user-secrets.

Rationale: a small codebase driven heavily by an AI agent stays maintainable only if conventions
are uniform and predictable.

## Quality Gates

A change MAY be merged only when all of the following hold:

1. `dotnet build` succeeds with no new warnings.
2. `dotnet test` (default suite) is green. Infrastructure-tagged tests required by Principle IV
   are run and green for the changes that trigger them.
3. Every acceptance criterion in the feature's `spec.md` is checked and mapped to a test; every
   task in `tasks.md` is done.
4. Spec documents affected by the change are updated in the same change (Principle I).
5. EF Core schema changes come with a new migration; applied migrations are never edited. Data
   remapping/backfill is done by explicit SQL inside the migration and covered by a test or a
   documented manual verification step.
6. No secrets, credentials or real financial data appear in code, tests, fixtures or logs.

## Development Workflow

- Work follows Spec-Driven Development: Specify → Clarify → Plan → Tasks → Implement. Open
  questions in `spec.md` MUST be resolved before `plan.md` is written.
- `plan.md` lists the tests to be written for each acceptance criterion and the fidelity level
  chosen for each (unit / SQLite / PostgreSQL / end-to-end), justified against Principle IV.
- Tests for a behavior are written together with (preferably before) its implementation, in the
  same change; "tests later" tasks are not accepted.
- Reviews (human or `/code-review`) MUST check compliance with this constitution; violations are
  either fixed or explicitly recorded as a justified exception in `plan.md`.
- A feature's `Status:` is set to `Implemented` and `specs/backlog.md` is updated only after all
  Quality Gates pass.

## Governance

- This constitution supersedes any conflicting practice, template or guidance file. Runtime
  development guidance lives in `CLAUDE.md` and the `specs/` documents, which MUST stay consistent
  with it.
- Amendments are made via `/speckit-constitution`, documented with a Sync Impact Report, reviewed,
  and committed as a dedicated change.
- Versioning follows semantic versioning: MAJOR for removing or redefining a principle in a
  backward-incompatible way; MINOR for a new principle/section or materially expanded guidance;
  PATCH for clarifications and wording.
- Compliance is verified at every review and at the end of every feature (Quality Gates). Any
  exception MUST be time-boxed or tracked in `specs/backlog.md`.

**Version**: 1.0.0 | **Ratified**: 2026-09-27 | **Last Amended**: 2026-09-27
