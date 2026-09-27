# MCP Tools & REST Contract

18 tools, all `[McpServerToolType]` classes in `FinancialMcp.Api/Mcp/Tools`, registered via `WithToolsFromAssembly()`. All thin (constitution §10). Enum params are integers (see [glossary](glossary.md#enums-sent-as-integers-over-mcp)). Write tools validate source format before persisting.

## CategoryTools

| Tool | Kind | Behavior |
|---|---|---|
| `list_categories` | Query | Distinct parent categories in use, each with distinct subcategories. No params. Doesn't imply a goal. |
| `lookup_category` | Query | Resolves a category for a `description` from the learned description→category mapping (updated on every categorizing create/update; most recent wins). Exact match today; fuzzy pending ([013](../features/013-lookup-category-fuzzy/spec.md)). |

## TransactionTools

| Tool | Kind | Behavior |
|---|---|---|
| `list_transactions` | Query | Filters: `periodStart`/`periodEnd` (required, inclusive on `ExpectedDate`), `type`, `status`, `category`, `subcategory`, `accountId`, `year`/`month`, `page`/`pageSize`. Ordered by `expectedDate` desc. ⚠ `subcategory`/`year`/`month` run in memory after paging, so `TotalCount` can exceed `Items.Count` (known, unfixed). |
| `get_transaction` | Query | Full detail by `transactionId`. 404 if unknown or soft-deleted. |
| `create_transaction` | Command | `invoiceDueDate` required for CD accounts; installment fields required when `recurrence = Installment`; `confirmationDate` required when `status = Confirmed`. Stamps initial status timestamp. |
| `update_transaction` | Command | Partial patch (`status`, `rawCategory`, `amount`, dates); `null` keeps stored value. Not patchable: `accountId`, `recurrence`, installment fields. |
| `delete_transaction` | Command | Soft delete. Requires `confirm = true` (validator). |
| `confirm_transaction` | Command | `Scheduled → Confirmed`; rejected unless current status is `Scheduled` (unknown id → 404). Sets `confirmationDate` for CC only. Publishes `TransactionConfirmedNotification`. |

Every `TransactionDto` carries `needsConfirmation`. Create/update/delete also carry `remainingBudget`/`remainingBudgetPercentage` (see [business rules](business-rules.md#post-write-budget-feedback)).

## CheckingAccountTools (read-only, never returns credit cards)

| Tool | Behavior |
|---|---|
| `list_checking_accounts` | All non-credit accounts, ordered by `displayName`, each with `creditCardIds` paid from it. |
| `get_checking_account` | One account by `accountId`. |

## CreditCardTools (read-only)

`CreditCard` is an `Account` (TPH, same table) with `ClosingDay`/`DueDay`/`PaymentAccountId`; `Kind` is always `Credit`, never settable.

| Tool | Behavior |
|---|---|
| `list_credit_cards` | All cards, ordered by `displayName`. |
| `get_credit_card` | One card by `creditCardId`. |

## BudgetTools

| Tool | Kind | Behavior |
|---|---|---|
| `get_budget_status` | Query | `year`/`month` required. Per category with a goal in effect: `GoalAmount`, `ActualSpend`, `RemainingBudget`, `PercentUsed`. |
| `create_category_budget` | Command | `categoryId` (parent), `year`/`month`, `amount`/`currencyCode`, `period`. |

## StatementTools

| Tool | Kind | Behavior |
|---|---|---|
| `import_statement` | Command | `csvContent` into `accountId`; CC/CD parsing inferred from `Account.Kind`. See [business rules](business-rules.md#statement-import). |

## RevisionTools

| Tool | Kind | Scope | Behavior |
|---|---|---|---|
| `list_revisions` | Query | read | Pending `transaction_revisions`, `page`/`pageSize`, optional `category`/`accountId`, ordered by `createdAt` asc. |
| `approve_revision` | Command | **`approval`** | `revisionId`, `confirm = true`. Moves revision → new `Scheduled` transaction atomically. Missing scope → 403 at tool level, before MediatR. 404 if unknown. |

## REST-only (minimal APIs in `Program.cs`)

`POST`/`PUT`/`DELETE /api/financial/checking-accounts` and `/api/financial/credit-cards`, dispatching the same MediatR commands (`Create|Update|Delete` `CheckingAccount|CreditCard`).

## CQRS index

- **Commands:** `create_transaction`, `update_transaction`, `delete_transaction`, `confirm_transaction`, `approve_revision`, `import_statement`, `create_category_budget`, REST account/card writes.
- **Queries:** `list_transactions`, `get_transaction`, `list_revisions`, `list_categories`, `lookup_category`, `get_budget_status`, `list_checking_accounts`, `get_checking_account`, `list_credit_cards`, `get_credit_card`.
