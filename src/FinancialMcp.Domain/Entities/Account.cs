using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>Checking account linked to a bank (checking account statement).</summary>
public class Account : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Bank { get; set; } = default!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
