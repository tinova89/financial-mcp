# Business Rules

This file is the source of truth for how calculations and queries work. The code must do what this file says. Change this file first.

## Categories

- We read the raw `Categoria` column of CC/CD statements and split it on `/` into `ParentCategory/Subcategory` (value object `Category`).
- A budget goal is always for a **parent** category, never for a subcategory. To calculate a goal, we add every row of that parent category. The subcategory does not matter.
- We can show a list by subcategory as extra information. A subcategory never has its own goal.
- `list_categories` only gives information. It does not mean that a goal exists.

## Transaction statuses

- Life cycle: `Revision → Scheduled → Confirmed`. Going back from `Confirmed → Scheduled` is also allowed (the tool is not built yet, see backlog).
- `Revision` rows are in `transaction_revisions`, not in `transactions`. Approval **moves** the row: it adds a new `transactions` row as `Scheduled` and deletes the revision, all in one atomic step.
- Each status writes its own timestamp one time and never changes it again (`Transaction.TransitionTo`):
  - `SubmittedForReviewAt` — copied exactly from `transaction_revisions.CreatedAt` on approval. We never create a new value.
  - `ScheduledAt`, `ConfirmedAt` — written when the transaction enters that status.
- To confirm a transaction, its current status must be `Scheduled`.
- `needsConfirmation = (Status == Scheduled && ExpectedDate < today)`. It only gives information. Nothing changes the status by itself.

## Budget goals (`get_budget_status`)

1. **Status filter:** only `Confirmed`.
2. **Type filter:** only `Expense`. Never `Income`, `Transfer` or `Payment` (we do not count the card payment in CC, so we do not count the same money two times).
3. **Reference month:**
   - CC: `ConfirmationDate`.
   - CD: `InvoiceDueDate` (not `ActualDate`). If it is on a weekend, we move it to the next business day.
4. **Which goal is active** (`BudgetGoal.ResolveEffective`):
   - `OneTime` — only for its own year/month.
   - `Monthly` — from its month and after, until a newer `Monthly` goal for the same category replaces it.
   - For its exact month, `OneTime` wins over `Monthly`.
5. **Formulas:**
   - `ActualSpend` = Σ |amount| of `Confirmed` `Expense` rows, with the same parent category and reference month, CC + CD together (`ActualSpendCalculator.SumForCategoryMonth`).
   - `RemainingBudget` = `GoalAmount − ActualSpend`.
   - `PercentUsed` = `ActualSpend / GoalAmount` (`null` when `GoalAmount = 0`).
6. Categories with no active goal do not appear in `get_budget_status`.

## Budget feedback after a write

- `create_transaction` / `update_transaction` / `delete_transaction` return `remainingBudget` / `remainingBudgetPercentage` for the parent category and reference month of the transaction. They use the same rules as above (`ICategoryBudgetRemainingCalculator`).
- Create/update count the amount of the transaction. Delete does not count it. The value is `null` when no goal is active.
- Read tools never fill these fields.

## Creating a budget goal

- We reject it if `categoryId` is a subcategory, or if a goal already exists for that category/month.

## Statement import

- CSV: `;` separator, `dd/mm/yyyy` dates, dot for decimals. We keep the Portuguese headers from the source (`Categoria`, `Data prevista`, …) as they are.
- We decide between CC and CD parsing from `Account.Kind`, never from a flag sent by the caller.
- We skip invalid lines and show a warning. The valid lines are saved together in one atomic step.
- Known problems (tests lock them in, they are not fixed): a wrong delimiter creates a row with all default values and no warning; after one row throws an error, we lose all the rows after it.

## Account group

- `Account.Group` is free text (for example `HOME`, `SOM`). On create, it comes from `X-Account-Group`. The caller never sends it as a field.
- **Not built yet:** separating read data by group. Today, two groups see the same data.
