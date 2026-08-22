namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Represents a financial transaction category.
/// </summary>
/// <remarks>Payment values are excluded from budget goal calculations to avoid double-counting.</remarks>
public enum TransactionType
{
    Expense = 1,
    Income = 2,
    Transfer = 3,
    Payment = 4 // "Card payment" — never enters the budget goal calculation (avoids double-counting).
}
