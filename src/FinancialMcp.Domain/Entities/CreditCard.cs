using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Credit card (Nubank, Bradesco, Sofisa, and future ones). A credit card is itself a kind
/// of Account (Kind is always FinancialAccountKind.Credit), mapped via EF Core Table-Per-Hierarchy
/// on the shared "accounts" table. PaymentAccount is the *other* account debited when the bill is paid.
/// </summary>
public class CreditCard : Account
{
    public byte ClosingDay { get; set; }              // day of the month the bill closes
    public byte DueDay { get; set; }                  // day of the month the bill is due

    public Guid PaymentAccountId { get; set; }          // Account debited when the bill is paid
    public Account PaymentAccount { get; set; } = default!;

    public ICollection<Transaction> CardTransactions { get; set; } = new List<Transaction>();
}
