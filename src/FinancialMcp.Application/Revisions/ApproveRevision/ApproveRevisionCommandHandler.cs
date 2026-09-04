using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Revisions.ApproveRevision;

/// <summary>
/// Single handler for <see cref="ApproveRevisionCommand"/>. Promotes a
/// <c>transaction_revisions</c> row to a new <c>transactions</c> row and removes the
/// revision — a <b>move</b>, not a copy. Persistence (the insert + the delete) is committed
/// atomically by <c>TransactionBehavior</c> because the command is an
/// <c>ITransactionalRequest</c>; this handler never calls <c>SaveChangesAsync</c> itself
/// (same pattern as <c>DeleteTransactionCommandHandler</c>).
/// </summary>
public sealed class ApproveRevisionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ApproveRevisionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(ApproveRevisionCommand request, CancellationToken cancellationToken)
    {
        var revision = await db.TransactionRevisions
            .Include(r => r.Category).ThenInclude(c => c.ParentCategory)
            .FirstOrDefaultAsync(r => r.Id == request.RevisionId, cancellationToken);

        if (revision is null)
        {
            throw new NotFoundException(nameof(TransactionRevision), request.RevisionId);
        }

        var now = DateTimeOffset.UtcNow;

        var transaction = new Transaction
        {
            Type = revision.Type,
            Description = revision.Description,
            Amount = revision.Amount,
            CategoryId = revision.CategoryId,
            ExpectedDate = revision.ExpectedDate,
            ActualDate = revision.ActualDate,
            ConfirmationDate = revision.ConfirmationDate,
            InvoiceDueDate = revision.InvoiceDueDate,
            Recurrence = revision.Recurrence,
            CurrentInstallment = revision.CurrentInstallment,
            TotalInstallments = revision.TotalInstallments,
            AccountId = revision.AccountId,

            // Copied verbatim from the revision's Revision-stage submission timestamp —
            // never regenerated, never "now" (Card #15 / see Transaction.SubmittedForReviewAt).
            SubmittedForReviewAt = revision.CreatedAt,
        };

        // Stamps Status = Scheduled + ScheduledAt = now (once). SubmittedForReviewAt is left
        // as set above — TransitionTo intentionally never touches it.
        transaction.TransitionTo(TransactionStatus.Scheduled, now);

        db.Transactions.Add(transaction);
        db.TransactionRevisions.Remove(revision);

        // Final SaveChangesAsync + commit/rollback is done by TransactionBehavior, so the
        // insert and the delete land together or not at all.

        var today = DateOnly.FromDateTime(now.UtcDateTime);

        return new TransactionDto(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Status.ToString(),
            transaction.Description,
            transaction.Amount,
            revision.Category.FullName,
            transaction.ExpectedDate,
            transaction.ActualDate,
            transaction.ConfirmationDate,
            transaction.InvoiceDueDate,
            transaction.Recurrence.ToString(),
            transaction.CurrentInstallment,
            transaction.TotalInstallments,
            transaction.AccountId,
            transaction.NeedsConfirmation(today));
    }
}
