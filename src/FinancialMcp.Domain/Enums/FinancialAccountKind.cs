namespace FinancialApp.Model;

/// <summary>
/// Classifies the account for reporting and business rules (e.g. credit vs debit vs investments).
/// </summary>
public enum FinancialAccountKind
{
    Credit = 1,
    Debit = 2,
    Investment = 3,
    Wallet = 4
}
