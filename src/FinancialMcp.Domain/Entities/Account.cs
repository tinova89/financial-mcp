using FinancialApp.Model;
using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>Checking account linked to a bank (checking account statement).</summary>
public class Account : BaseEntity
{
    public string DisplayName { get; set; } = default!;
    public string BankCode { get; set; } = default!;

    /// <summary>
    /// Separates accounts by group (e.g. "HOME", "SOM"). Every request in the system must
    /// supply this via the X-Account-Group HTTP header (see RequireGroupHeaderMiddleware);
    /// stamped from that header on creation, never a caller-supplied command field.
    /// </summary>
    public string Group { get; set; } = default!;

    public decimal InitialAmount { get; set; } = default!;

    /// <summary>
    /// Computed from the entity's own runtime type (the EF Core TPH "AccountType"
    /// discriminator), never persisted — see AccountConfiguration.Ignore(e => e.Kind).
    /// CreditCard overrides this to always return Credit.
    /// </summary>
    public virtual FinancialAccountKind Kind => FinancialAccountKind.Debit;

    public string BaseCurrencyCode { get; set; } = default!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
}
