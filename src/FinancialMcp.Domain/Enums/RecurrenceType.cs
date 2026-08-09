namespace FinancialMcp.Domain.Enums;

/// <summary>
/// "Repetição" column from the card statement. See CLAUDE.md > Business Rules >
/// Credit card — billing cycle, installments and projection.
/// </summary>
public enum RecurrenceType
{
    None,
    Installment,
    FixedMonthly
}
