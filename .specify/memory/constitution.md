<!--
Sync Impact Report
==================
Version change: 1.0.0 → 1.0.1
Bump rationale: PATCH. We rewrote the text in simple English (A2 level). The meaning of the
principles, gates and rules did not change.

Modified principles: none (only wording).
Added sections: none
Removed sections: none

Templates that need updates:
  ✅ .specify/templates/* — not changed (they read the constitution at runtime).

Follow-up TODOs:
  - Quality Gate "dotnet test is green" conflicts with the known HealthCheck test failure that
    needs a running Postgres. We must tag/isolate that test as infrastructure-dependent (see
    Principle IV) so the default run can be green.
-->

# FinancialMcp Constitution

## Core Principles

### I. Spec Is the Source of Truth

- The specs (`specs/`, feature `spec.md`/`plan.md`/`tasks.md`) and the domain rule documents
  define the behavior. We write the code from them, never the other way.
- A change to behavior that people can see (MCP tool contract, REST contract, business rule,
  calculation, enum value, validation rule) MUST update the spec document for it **in the same
  change**, before the code or together with it. A spec that is older than the code is a bug.
- Known differences between the docs and the code (for example, a parser problem that a test
  locks in) MUST be written clearly in the place where the rule is documented. Do not leave them
  hidden.

Why: Claude and people both understand this system through its specs. Old specs cause wrong
calculations and wrong use of the tools later.

### II. Thin Entry Points, Isolated Rules

- MCP tools and REST endpoints MUST only build a MediatR request and call `Send`/`Publish`. A
  tool, endpoint or controller has no business rules, no query logic and no validation.
- Each use case is one request + one handler (+ validator) in its own feature folder. They are
  split into Commands (writes) and Queries (reads).
- Pure calculation rules (budget totals, reference month, next business day, category parsing,
  installments) MUST be in injectable domain/application services or value objects. Then we can
  unit test them without a handler, a database or a transport.
- Shared concerns (logging, validation, database transactions) MUST be built one time as pipeline
  behaviors. Do not repeat them in each handler.

Why: when rules are separate from transport and persistence, they are cheap to test and safe to
change.

### III. Every Behavior Is Tested (NON-NEGOTIABLE)

- A feature is not done until each acceptance criterion in its `spec.md` has at least one
  automated test. That test must fail if the criterion is broken.
- Every handler, validator and domain service MUST have tests for: the success path, each
  validation/guard rejection, the not-found path (when there is one), and every documented edge
  case.
- Every bug fix MUST add a regression test. It fails before the fix and passes after it.
- Known problems that we decide not to fix MUST be locked in by a test. The test shows the current
  behavior and says where the problem is tracked. So any change to it is a conscious decision.
- Tests use the project's standard tools — xUnit, FluentAssertions, NSubstitute — and follow
  Arrange/Act/Assert. Test names say the scenario and the expected result.
- Tests MUST always give the same result: they must not depend on the real clock ("today"), the
  order they run in, shared state that changes, or an external network. Time is injected (for
  example `TimeProvider`/a clock abstraction).

Why: this system works with money data for an AI agent. Behavior with no tests is behavior that
nobody checked.

### IV. Test Fidelity Matches the Risk

- Unit tests (mocks/substitutes) are the default for pure rules and for handler orchestration.
- Use SQLite `:memory:` when a test needs real transactions or real EF Core query filters (soft
  delete, TPH scoping). Do NOT use EF Core's `InMemory` provider for these.
- A real PostgreSQL integration test (`Testcontainers.PostgreSql`) is REQUIRED — a green SQLite
  test is not enough proof — when the code:
  - adds up money values in SQL (`SumAsync`, a translated `GroupBy`, etc.) instead of loading the
    rows and adding them in C# `decimal`;
  - depends on writes at the same time, transaction isolation, locks or Npgsql retry behavior;
  - depends on types, functions, collations or migration SQL that only PostgreSQL has.
- Each exposed MCP tool SHOULD have at least one end-to-end test with `TestServer` and a real MCP
  client. It covers auth, the `X-Account-Group` header and the tool contract.
- Tests that need external infrastructure (Docker, Postgres) MUST have a tag. Then we can choose
  to run them or skip them, and they never cause random failures in the default fast test run.

Why: SQLite and PostgreSQL work differently when they add numbers and when many writes happen at
the same time. You MUST write the cheapest test that can really find the risk.

### V. Financial Correctness by Construction

- Money is always `decimal` in C# and `numeric` in PostgreSQL. `double`/`float` are not allowed
  for amounts in any layer, also in tests and in calculations in the middle.
- Dates use real date/time types (`DateOnly`/`DateTimeOffset` → `date`/`timestamptz`), never
  strings. Each shared rule (next business day, `ParentCategory/Subcategory` parsing,
  `ActualSpend`) has only one implementation, and we use it everywhere.
- Every write MUST be validated (FluentValidation, through the pipeline) before we save it.
  Imports MUST check the source format and report the lines they skip. They must not fail
  silently.
- Destructive operations need an explicit `confirm = true`, checked by validation. They use soft
  delete behind a global query filter.
- Entities never leave the application layer. Transports only see DTOs/records.

Why: losing precision without a warning, dates that move, or writes with no validation make wrong
budgets. Nobody sees them until reconciliation.

### VI. Maintainability and Simplicity

- Nullable reference types on; file-scoped namespaces; `async`/`await` from start to end for I/O,
  with the `Async` suffix; immutable `record` requests.
- Same naming everywhere: `<Action><Entity>Command`/`Query`, `<Name>Handler`, `<Name>Validator`;
  feature folders, not general `Commands/`/`Handlers/` folders.
- Only one source of truth for shared constants (header names, scopes, max lengths, enum
  mappings). Do not copy the same literal in many layers.
- Build with zero new compiler warnings. Do not turn off warnings or analyzers without a comment
  that explains why.
- YAGNI: no abstractions, options or layers "for the future" if a spec does not ask for them. If
  you add complexity, you MUST explain why in the feature's `plan.md`.
- Remove dead code, commented-out code and old TODOs, or track them in `specs/backlog.md`. Do not
  leave them in the code. Temporary exceptions (for example, auth middleware that is turned off)
  MUST be documented where a reader of that code will find them.
- Hardcoded ports, URLs, connection strings and secrets are not allowed. Use Aspire service
  discovery, configuration and user-secrets.

Why: a small codebase that an AI agent changes a lot stays easy to maintain only if the rules are
the same everywhere and easy to predict.

## Quality Gates

We MAY merge a change only when all of these are true:

1. `dotnet build` works with no new warnings.
2. `dotnet test` (default suite) is green. The infrastructure-tagged tests that Principle IV
   requires are run and green for the changes that need them.
3. Every acceptance criterion in the feature's `spec.md` is checked and has a test. Every task in
   `tasks.md` is done.
4. The spec documents that the change affects are updated in the same change (Principle I).
5. EF Core schema changes come with a new migration. We never edit a migration that was already
   applied. Data remapping/backfill uses explicit SQL inside the migration, and a test or a
   documented manual check covers it.
6. No secrets, credentials or real financial data are in code, tests, fixtures or logs.

## Development Workflow

- Work follows Spec-Driven Development: Specify → Clarify → Plan → Tasks → Implement. Open
  questions in `spec.md` MUST be answered before we write `plan.md`.
- `plan.md` lists the tests to write for each acceptance criterion and the fidelity level for each
  one (unit / SQLite / PostgreSQL / end-to-end), with a reason based on Principle IV.
- We write the tests for a behavior together with its code (better: before it), in the same
  change. We do not accept "tests later" tasks.
- Reviews (by a person or `/code-review`) MUST check that the change follows this constitution.
  Problems are fixed, or written in `plan.md` as an exception with a reason.
- We set a feature's `Status:` to `Implemented` and update `specs/backlog.md` only after all
  Quality Gates pass.

## Governance

- This constitution is more important than any practice, template or guide that says something
  different. Day-to-day development guidance is in `CLAUDE.md` and the `specs/` documents, and
  they MUST agree with it.
- Changes to it are made with `/speckit-constitution`, documented with a Sync Impact Report,
  reviewed, and committed as their own change.
- Versions follow semantic versioning: MAJOR when we remove a principle or change its meaning in
  a way that breaks the old one; MINOR for a new principle/section or much more guidance; PATCH
  for clarifications and wording.
- We check that work follows it at every review and at the end of every feature (Quality Gates).
  Every exception MUST have an end date or be tracked in `specs/backlog.md`.

**Version**: 1.0.1 | **Ratified**: 2026-09-27 | **Last Amended**: 2026-10-01
