using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.DeleteTransaction;

/// <summary>
/// Single handler for DeleteTransactionCommand. The confirmation has already been
/// validated by ValidationBehavior before this handler is reached; here it only applies the soft delete.
/// </summary>
public sealed class DeleteTransactionCommandHandler(IApplicationDbContext db, ICategoryBudgetRemainingCalculator budgetRemainingCalculator)
    : IRequestHandler<DeleteTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions
            .Include(t => t.Category).ThenInclude(c => c.ParentCategory)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException(nameof(Transaction), request.TransactionId);
        }

        transaction.MarkAsDeleted();

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        var budgetRemaining = await budgetRemainingCalculator.CalculateAsync(transaction, includeTransaction: false, cancellationToken);

        return new TransactionDto(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Status.ToString(),
            transaction.Description,
            transaction.Amount,
            transaction.Category.FullName,
            transaction.ExpectedDate,
            transaction.ActualDate,
            transaction.ConfirmationDate,
            transaction.InvoiceDueDate,
            transaction.Recurrence.ToString(),
            transaction.CurrentInstallment,
            transaction.TotalInstallments,
            transaction.AccountId,
            transaction.NeedsConfirmation(DateOnly.FromDateTime(DateTime.UtcNow)),
            budgetRemaining.RemainingBudget,
            budgetRemaining.RemainingBudgetPercentage);
    }
}
