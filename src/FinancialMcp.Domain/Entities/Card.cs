using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>Credit card (Nubank, Bradesco, Sofisa, and future ones), linked to a payment Account.</summary>
public class Card : BaseEntity
{
    public string Name { get; set; } = default!;    // e.g.: "Nubank", "Bradesco", "Sofisa"
    public byte ClosingDay { get; set; }              // day of the month the bill closes
    public byte DueDay { get; set; }                  // day of the month the bill is due

    public Guid AccountId { get; set; }                // Account debited when the bill is paid
    public Account Account { get; set; } = default!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
