using FinancialMcp.Domain.Enums;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// A proposed set of <see cref="Transaction"/> field values submitted while the transaction
/// sits in <see cref="TransactionStatus.Revision"/>, awaiting review (Card #14). Same shape as
/// <see cref="Transaction"/> minus the generated/identity plumbing (<c>UpdatedAt</c>,
/// <c>IsDeleted</c>/<c>DeletedAt</c>, and the per-status <c>ScheduledAt</c>/<c>ConfirmedAt</c>/
/// <c>SubmittedForReviewAt</c> stamps).
///
/// <see cref="CreatedAt"/> is reused as the Revision-stage submission timestamp — there is no
/// separate <c>SubmittedForReviewAt</c> column here. On approval (out of scope for Card #14)
/// this value is copied verbatim into <see cref="Transaction.SubmittedForReviewAt"/>.
///
/// Every free-text field is capped at 256 characters (see CLAUDE.md > Code Conventions).
/// </summary>
public sealed class TransactionRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The transaction this revision was submitted against.</summary>
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = default!;

    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }

    public string Description { get; set; } = default!;

    public decimal Amount { get; set; }

    public Guid CategoryId { get; set; }
    public TransactionCategory Category { get; set; } = default!;

    public DateOnly ExpectedDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public DateOnly? ConfirmedDate { get; set; }
    public DateOnly? InvoiceDueDate { get; set; }

    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
    public int? CurrentInstallment { get; set; }
    public int? TotalInstallments { get; set; }

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    /// <summary>Submission timestamp for the Revision stage (reused from the base entity's CreatedAt).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
