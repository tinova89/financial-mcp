using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

public sealed class UpdateTransactionCommandHandler(
    IApplicationDbContext db,
    ITransactionCategoryResolver categoryResolver,
    ICategoryBudgetRemainingCalculator budgetRemainingCalculator)
    : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var t = await db.Transactions
            .Include(x => x.Category).ThenInclude(c => c.ParentCategory)
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (t is null)
        {
            throw new NotFoundException(nameof(Transaction), request.TransactionId);
        }

        var now = DateTimeOffset.UtcNow;

        // Stamps ScheduledAt/ConfirmedAt the first time that status is entered (Card #14).
        if (request.Status is not null) t.TransitionTo(request.Status.Value, now);

        if (request.RawCategory is not null)
        {
            t.RawCategory = request.RawCategory;
            await categoryResolver.ResolveAsync(t, cancellationToken);
        }

        if (request.Amount is not null) t.Amount = request.Amount.Value;
        if (request.ExpectedDate is not null) t.ExpectedDate = request.ExpectedDate.Value;
        if (request.ActualDate is not null) t.ActualDate = request.ActualDate;
        if (request.ConfirmationDate is not null) t.ConfirmationDate = request.ConfirmationDate;
        if (request.InvoiceDueDate is not null) t.InvoiceDueDate = request.InvoiceDueDate;

        t.UpdatedAt = now;

        var budgetRemaining = await budgetRemainingCalculator.CalculateAsync(t, includeTransaction: true, cancellationToken);

        return new TransactionDto(
            t.Id, t.Type.ToString(), t.Status.ToString(), t.Description, t.Amount,
            t.Category.FullName, t.ExpectedDate, t.ActualDate, t.ConfirmationDate, t.InvoiceDueDate,
            t.Recurrence.ToString(), t.CurrentInstallment, t.TotalInstallments, t.AccountId,
            t.NeedsConfirmation(DateOnly.FromDateTime(now.UtcDateTime)),
            budgetRemaining.RemainingBudget, budgetRemaining.RemainingBudgetPercentage);
    }
}
