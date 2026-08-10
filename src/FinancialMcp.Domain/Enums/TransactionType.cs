namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Represents a financial transaction category.
/// </summary>
/// <remarks>Payment values are excluded from budget goal calculations to avoid double-counting.</remarks>
public enum TransactionType
{
    Expense,
    Income,
    Transfer,
    Payment // "Card payment" — never enters the budget goal calculation (avoids double-counting).
}
