using FinancialApp.Model;
using FinancialMcp.Domain.Common;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// A line from a Checking Account or Credit Card statement. See CLAUDE.md >
/// Financial Domain Business Rules for the aggregation/projection rules that
/// consume this entity.
/// </summary>
public class Transaction : BaseEntity
{
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }

    public string Description { get; set; } = default!;

    /// <summary>Always decimal — never double/float (see CLAUDE.md > Code Conventions > Money).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// "Categoria-mãe/Subcategoria" as it came from the statement/caller — support-only,
    /// never persisted (see TransactionConfiguration.Ignore). Used solely to resolve
    /// CategoryId/Category via ITransactionCategoryResolver at write time; reads must go
    /// through Category (e.g. Category.FullName), not this property.
    /// </summary>
    public string RawCategory { get; set; } = default!;

    public Guid CategoryId { get; set; }

    /// <summary>
    /// Persisted category/subcategory this transaction belongs to. Resolved (get-or-created)
    /// from RawCategory by ITransactionCategoryResolver whenever a transaction is created,
    /// imported, or has its RawCategory changed.
    /// </summary>
    public TransactionCategory Category { get; set; } = default!;

    /// <summary>Splits RawCategory into (ParentCategory, Subcategory) — see CLAUDE.md > Category/Subcategory.</summary>
    public (string ParentCategory, string? Subcategory) SplitRawCategory()
    {
        var parsed = FinancialMcp.Domain.ValueObjects.Category.Parse(RawCategory);
        return (parsed.ParentCategory, parsed.Subcategory);
    }

    // Explicit dates (never string) — see CLAUDE.md > Code Conventions > Dates.
    public DateOnly ExpectedDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public DateOnly? ReconciledDate { get; set; }   // checking account only, when Status = Reconciled
    public DateOnly? InvoiceDueDate { get; set; }   // credit card only — date that impacts the balance

    // Installments / recurrence (credit card only).
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
    public int? CurrentInstallment { get; set; }
    public int? TotalInstallments { get; set; }

    /// <summary>
    /// Always set. For checking-account-sourced transactions, the checking account itself;
    /// for credit-card-sourced transactions, the CreditCard's own row (valid since CreditCard
    /// is an Account via EF Core TPH) — the single relationship identifies both.
    /// </summary>
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    /// <summary>
    /// Reference Mês_Ano: "Data Conciliado" for reconciled checking account, "Venc. Fatura" for credit card
    /// (see CLAUDE.md > Business Rules > Budget goals, item 3). Uses Account.Kind — the
    /// authoritative, always-correct discriminator — rather than the caller-supplied Source.
    /// Requires Account to be loaded (callers must .Include(t => t.Account)).
    /// </summary>
    public MonthYear? GetReferenceMonthYear()
    {
        if (Account.Kind != FinancialAccountKind.Credit && Status == TransactionStatus.Reconciled && ReconciledDate is not null)
        {
            return MonthYear.FromDate(ReconciledDate.Value);
        }

        if (Account.Kind == FinancialAccountKind.Credit && InvoiceDueDate is not null)
        {
            return MonthYear.FromDate(InvoiceDueDate.Value);
        }

        return null;
    }

    /// <summary>Counts toward the Gasto_Real calculation for goals (see CLAUDE.md > Budget goals, items 1-2).</summary>
    public bool IsEligibleForActualSpend =>
        Status == TransactionStatus.Reconciled
        && Type == TransactionType.Expense;
}
