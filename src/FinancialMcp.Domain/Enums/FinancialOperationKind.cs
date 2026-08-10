namespace FinancialApp.Model;

/// <summary>
/// Whether the transaction is money out (expense) or money in (income / receita).
/// </summary>
public enum FinancialOperationKind
{
    Expense = 1,
    Income = 2,
    Transfer = 3,
}
