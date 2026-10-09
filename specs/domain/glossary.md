# Glossary

This is the only place where we define terms. Old names must not be in domain code (only in the CSV import mapping).

## Terms

| Term | Meaning | Old names |
|---|---|---|
| `ActualSpend` | Total of confirmed expenses for a parent category in a reference month | `Gasto_Real` |
| `GoalAmount` | The target amount of a budget goal | `Meta_Valor`, `BudgetAmount` |
| `RemainingBudget` | `GoalAmount − ActualSpend` | `Saldo_Meta` |
| `PercentUsed` | `ActualSpend / GoalAmount` | `% Utilizado` |
| `ExpectedDate` | The planned date of a transaction | `Data prevista` |
| `ActualDate` | The date it really happened | `Data efetiva` |
| `ConfirmationDate` | The date a CC transaction was confirmed; it decides the CC reference month | `Data Conciliado`, `ConfirmedDate`, `reconciledDate` |
| `InvoiceDueDate` | The due date of the card bill; it decides the CD reference month | `Venc. Fatura` |
| `ParentCategory` / `Subcategory` | The two parts of `Categoria`, split on `/` | `Categoria-mãe` / `Subcategoria` |
| `needsConfirmation` | `Scheduled` and `ExpectedDate < today` | — |
| `hints` | Description text that `lookup_category` uses to find a match (Card #13, not built yet) | — |
| Reference month | The year/month where a transaction counts in budgets | `Mês_Ano` |
| Account group | A way to split accounts into groups with `X-Account-Group` | — |
| CC | Checking account (an `Account` that is not credit) | — |
| CD | Credit card (`CreditCard`, TPH on `accounts`) | — |

## Enums (sent as numbers over MCP)

| Enum | Values | Old |
|---|---|---|
| `TransactionStatus` | `1 Revision`, `2 Scheduled`, `3 Confirmed` | `Agendado`, `Nconciliado` → Scheduled; `Conciliado` → Confirmed |
| `TransactionType` | `1 Expense`, `2 Income`, `3 Transfer`, `4 Payment` | `Despesa`, `Receita`, `Transferência`, `Pagamento` |
| `RecurrenceType` | `0 None`, `1 Installment`, `2 FixedMonthly` | — |
| `BudgetPeriodType` | `1 Monthly`, `2 OneTime` | — |

## Old tool / type names

| Now | Before |
|---|---|
| `confirm_transaction`, `ConfirmTransaction*`, `TransactionConfirmedNotification` | `reconcile_transaction`, `ReconcileTransaction*`, `TransactionReconciledNotification` |
| `list_checking_accounts`, `get_checking_account` (`CheckingAccountTools`) | `list_accounts`, `get_account` (`AccountTools`) |
