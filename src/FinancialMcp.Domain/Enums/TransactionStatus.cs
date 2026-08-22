namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Transaction status. Checking account uses Reconciled/Scheduled; credit card uses Reconciled/Unreconciled.
/// See CLAUDE.md > Business Rules > Budget goals (status filter).
/// </summary>
public enum TransactionStatus
{
    Reconciled = 1,
    Scheduled = 2,    // checking account only
    Unreconciled = 3  // credit card only — expected, subject to change until the bill closes
}
