namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Transaction status. Checking account uses Reconciled/Scheduled; credit card uses Reconciled/Unreconciled.
/// See CLAUDE.md > Business Rules > Budget goals (status filter).
/// </summary>
public enum TransactionStatus
{
    Reconciled,
    Scheduled,    // checking account only
    Unreconciled  // credit card only — expected, subject to change until the bill closes
}
