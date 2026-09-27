# Glossary

The only place terms are defined. Retired names must not appear in domain code (only in the CSV import mapping).

## Terms

| Term | Meaning | Retired names |
|---|---|---|
| `ActualSpend` | Sum of confirmed expenses for a parent category in a reference month | `Gasto_Real` |
| `GoalAmount` | Target amount of a budget goal | `Meta_Valor`, `BudgetAmount` |
| `RemainingBudget` | `GoalAmount − ActualSpend` | `Saldo_Meta` |
| `PercentUsed` | `ActualSpend / GoalAmount` | `% Utilizado` |
| `ExpectedDate` | Planned date of a transaction | `Data prevista` |
| `ActualDate` | Date it actually happened | `Data efetiva` |
| `ConfirmationDate` | Date a CC transaction was confirmed; drives CC reference month | `Data Conciliado`, `ConfirmedDate`, `reconciledDate` |
| `InvoiceDueDate` | Card invoice due date; drives CD reference month | `Venc. Fatura` |
| `ParentCategory` / `Subcategory` | Two halves of `Categoria` split on `/` | `Categoria-mãe` / `Subcategoria` |
| `needsConfirmation` | `Scheduled` and `ExpectedDate < today` | — |
| `hints` | Description text used by `lookup_category` matching (Card #13, pending) | — |
| Reference month | Year/month a transaction counts toward in budgets | `Mês_Ano` |
| Account group | Partition of accounts via `X-Account-Group` | — |
| CC | Checking account (non-credit `Account`) | — |
| CD | Credit card (`CreditCard`, TPH on `accounts`) | — |

## Enums (sent as integers over MCP)

| Enum | Values | Retired |
|---|---|---|
| `TransactionStatus` | `1 Revision`, `2 Scheduled`, `3 Confirmed` | `Agendado`, `Nconciliado` → Scheduled; `Conciliado` → Confirmed |
| `TransactionType` | `1 Expense`, `2 Income`, `3 Transfer`, `4 Payment` | `Despesa`, `Receita`, `Transferência`, `Pagamento` |
| `RecurrenceType` | `0 None`, `1 Installment`, `2 FixedMonthly` | — |
| `BudgetPeriodType` | `1 Monthly`, `2 OneTime` | — |

## Retired tool / type names

| Now | Was |
|---|---|
| `confirm_transaction`, `ConfirmTransaction*`, `TransactionConfirmedNotification` | `reconcile_transaction`, `ReconcileTransaction*`, `TransactionReconciledNotification` |
| `list_checking_accounts`, `get_checking_account` (`CheckingAccountTools`) | `list_accounts`, `get_account` (`AccountTools`) |
