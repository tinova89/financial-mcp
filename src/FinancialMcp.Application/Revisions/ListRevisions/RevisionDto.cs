namespace FinancialMcp.Application.Revisions.ListRevisions;

/// <summary>
/// Response DTO for <c>list_revisions</c>: the same shape as
/// <see cref="Transactions.CreateTransaction.TransactionDto"/> (never expose the
/// <c>TransactionRevision</c> domain entity directly — see CLAUDE.md &gt; DTOs), plus the
/// parent <see cref="TransactionId"/> and the Revision-stage submission timestamp
/// <see cref="CreatedAt"/> (the value that is copied verbatim into
/// <c>Transaction.SubmittedForReviewAt</c> on approval).
/// </summary>
public sealed record RevisionDto(
    Guid Id,
    Guid TransactionId,
    string Type,
    string Status,
    string Description,
    decimal Amount,
    string RawCategory,
    DateOnly ExpectedDate,
    DateOnly? ActualDate,
    DateOnly? ConfirmedDate,
    DateOnly? InvoiceDueDate,
    string? Recurrence,
    int? CurrentInstallment,
    int? TotalInstallments,
    Guid AccountId,
    DateTimeOffset CreatedAt);
