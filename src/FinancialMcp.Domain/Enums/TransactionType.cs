namespace FinancialMcp.Domain.Enums;

/// <summary>"Tipo" column from the statement. See CLAUDE.md > Business Rules > Budget goals.</summary>
public enum TransactionType
{
    Expense,
    Income,
    Transfer,
    Payment // "Pagamento de cartão" — never enters the budget goal calculation (avoids double-counting).
}
