using FinancialApp.Model;
using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>Checking account linked to a bank (checking account statement).</summary>
public class Account : BaseEntity
{
    public string DisplayName { get; set; } = default!;
    public string BankCode { get; set; } = default!;
    public decimal InitialAmount { get; set; } = default!;
    public FinancialAccountKind Kind { get; set; } = default!;
    public string BaseCurrencyCode { get; set; } = default!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
