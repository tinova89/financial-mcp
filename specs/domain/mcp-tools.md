# MCP Tools & REST Contract

There are 18 tools. All are `[McpServerToolType]` classes in `FinancialMcp.Api/Mcp/Tools`, registered with `WithToolsFromAssembly()`. All of them are thin (constitution §10). Enum parameters are numbers (see [glossary](glossary.md#enums-sent-as-numbers-over-mcp)). Write tools check the source format before they save anything.

## CategoryTools

| Tool | Kind | Behavior |
|---|---|---|
| `list_categories` | Query | The different parent categories in use, each with its different subcategories. No parameters. It does not mean that a goal exists. |
| `lookup_category` | Query | Finds a category for a `description` from the learned description→category list (it is updated on every create/update that sets a category; the newest one wins). Today it needs an exact match; fuzzy match is not built yet ([013](../features/013-lookup-category-fuzzy/spec.md)). |

## TransactionTools

| Tool | Kind | Behavior |
|---|---|---|
| `list_transactions` | Query | Filters: `periodStart`/`periodEnd` (required, both dates included, on `ExpectedDate`), `type`, `status`, `category`, `subcategory`, `accountId`, `year`/`month`, `page`/`pageSize`. Sorted by `expectedDate`, newest first. ⚠ `subcategory`/`year`/`month` run in memory after the page is made, so `TotalCount` can be bigger than `Items.Count` (known, not fixed). |
| `get_transaction` | Query | All the details for one `transactionId`. 404 if the id does not exist or was soft-deleted. |
| `create_transaction` | Command | `invoiceDueDate` is required for CD accounts; installment fields are required when `recurrence = Installment`; `confirmationDate` is required when `status = Confirmed`. Writes the timestamp of the first status. |
| `update_transaction` | Command | Changes only some fields (`status`, `rawCategory`, `amount`, dates); `null` keeps the saved value. You cannot change: `accountId`, `recurrence`, installment fields. |
| `delete_transaction` | Command | Soft delete. Needs `confirm = true` (validator). |
| `confirm_transaction` | Command | `Scheduled → Confirmed`; rejected if the current status is not `Scheduled` (unknown id → 404). Sets `confirmationDate` only for CC. Publishes `TransactionConfirmedNotification`. |

Every `TransactionDto` has `needsConfirmation`. Create/update/delete also have `remainingBudget`/`remainingBudgetPercentage` (see [business rules](business-rules.md#budget-feedback-after-a-write)).

## CheckingAccountTools (read-only, never returns credit cards)

| Tool | Behavior |
|---|---|
| `list_checking_accounts` | All accounts that are not credit, sorted by `displayName`. Each one has the `creditCardIds` that it pays. |
| `get_checking_account` | One account by `accountId`. |

## CreditCardTools (read-only)

A `CreditCard` is an `Account` (TPH, same table) with `ClosingDay`/`DueDay`/`PaymentAccountId`. `Kind` is always `Credit`, and you cannot set it.

| Tool | Behavior |
|---|---|
| `list_credit_cards` | All cards, sorted by `displayName`. |
| `get_credit_card` | One card by `creditCardId`. |

## BudgetTools

| Tool | Kind | Behavior |
|---|---|---|
| `get_budget_status` | Query | `year`/`month` are required. For each category with an active goal: `GoalAmount`, `ActualSpend`, `RemainingBudget`, `PercentUsed`. |
| `create_category_budget` | Command | `categoryId` (parent), `year`/`month`, `amount`/`currencyCode`, `period`. |

## StatementTools

| Tool | Kind | Behavior |
|---|---|---|
| `import_statement` | Command | Puts `csvContent` into `accountId`; CC/CD parsing comes from `Account.Kind`. See [business rules](business-rules.md#statement-import). |

## RevisionTools

| Tool | Kind | Scope | Behavior |
|---|---|---|---|
| `list_revisions` | Query | read | Pending `transaction_revisions`, `page`/`pageSize`, optional `category`/`accountId`, sorted by `createdAt`, oldest first. |
| `approve_revision` | Command | **`approval`** | `revisionId`, `confirm = true`. Moves the revision → a new `Scheduled` transaction, in one atomic step. No scope → 403 in the tool, before MediatR. 404 if the id does not exist. |

## REST only (minimal APIs in `Program.cs`)

`POST`/`PUT`/`DELETE /api/financial/checking-accounts` and `/api/financial/credit-cards`. They send the same MediatR commands (`Create|Update|Delete` `CheckingAccount|CreditCard`).

## CQRS index

- **Commands:** `create_transaction`, `update_transaction`, `delete_transaction`, `confirm_transaction`, `approve_revision`, `import_statement`, `create_category_budget`, REST writes for accounts/cards.
- **Queries:** `list_transactions`, `get_transaction`, `list_revisions`, `list_categories`, `lookup_category`, `get_budget_status`, `list_checking_accounts`, `get_checking_account`, `list_credit_cards`, `get_credit_card`.
