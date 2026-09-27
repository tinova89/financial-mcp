# Business Rules

Source of truth for calculation and query behavior. Code must match this file; change this file first.

## Categories

- The raw `Categoria` column of CC/CD statements is parsed into `ParentCategory/Subcategory`, split on `/` (value object `Category`).
- A budget goal always targets a **parent** category, never a subcategory. Aggregation for a goal sums every row of that parent category, regardless of subcategory.
- A subcategory breakdown may be produced as secondary information; it never has its own goal.
- `list_categories` is informational only; it does not imply a goal exists.

## Transaction statuses

- Lifecycle: `Revision → Scheduled → Confirmed`, with `Confirmed → Scheduled` allowed as a reversal (tool pending, see backlog).
- `Revision` rows live in `transaction_revisions`, not in `transactions`. Approval **moves** the row (insert new `transactions` row as `Scheduled`, delete the revision) atomically.
- Each status stamps its own timestamp once and never overwrites it (`Transaction.TransitionTo`):
  - `SubmittedForReviewAt` — copied verbatim from `transaction_revisions.CreatedAt` on approval, never regenerated.
  - `ScheduledAt`, `ConfirmedAt` — stamped on entering that status.
- Confirming requires current status `Scheduled`.
- `needsConfirmation = (Status == Scheduled && ExpectedDate < today)`. Informational only; nothing changes status automatically.

## Budget goals (`get_budget_status`)

1. **Status filter:** only `Confirmed`.
2. **Type filter:** only `Expense`. Never `Income`, `Transfer` or `Payment` (card payment in CC is excluded to avoid double counting).
3. **Reference month:**
   - CC: `ConfirmationDate`.
   - CD: `InvoiceDueDate` (not `ActualDate`), rolled to the next business day when it falls on a weekend.
4. **Goal in effect** (`BudgetGoal.ResolveEffective`):
   - `OneTime` — only its own year/month.
   - `Monthly` — from its month onward, until a later `Monthly` for the same category supersedes it.
   - `OneTime` beats `Monthly` for its exact month.
5. **Formulas:**
   - `ActualSpend` = Σ |amount| of `Confirmed` `Expense` rows, same parent category and reference month, CC + CD combined (`ActualSpendCalculator.SumForCategoryMonth`).
   - `RemainingBudget` = `GoalAmount − ActualSpend`.
   - `PercentUsed` = `ActualSpend / GoalAmount` (`null` when `GoalAmount = 0`).
6. Categories without a goal in effect don't appear in `get_budget_status`.

## Post-write budget feedback

- `create_transaction` / `update_transaction` / `delete_transaction` return `remainingBudget` / `remainingBudgetPercentage` for the transaction's parent category and reference month, using the same rules as above (`ICategoryBudgetRemainingCalculator`).
- Create/update include the transaction's own amount; delete excludes it. `null` when no goal is in effect.
- Read tools never populate these fields.

## Budget goal creation

- Rejected if `categoryId` is a subcategory or a goal already exists for that category/month.

## Statement import

- CSV: `;` separator, `dd/mm/yyyy` dates, dot decimal. Portuguese source headers (`Categoria`, `Data prevista`, …) are kept as-is.
- CC vs CD parsing is inferred from `Account.Kind`, never from a caller flag.
- Invalid lines are skipped with a warning; the valid batch commits atomically.
- Known gaps (locked by tests, not fixed): wrong delimiter yields an all-default row instead of a warning; after one row throws, the remaining rows are dropped.

## Account group

- `Account.Group` is free text (e.g. `HOME`, `SOM`), stamped from `X-Account-Group` on create; never a caller field.
- **Not yet implemented:** read isolation by group. Two groups currently see the same data.
