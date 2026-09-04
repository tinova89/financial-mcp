using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Single handler for CreateTransactionCommand — orchestrates persistence of the new
/// transaction. Calculation rules (installments, billing cycle) don't apply here:
/// each row already represents a concrete transaction (see CLAUDE.md > Mediator Pattern).
/// </summary>
public sealed class CreateTransactionCommandHandler(
    IApplicationDbContext db,
    ITransactionCategoryResolver categoryResolver,
    ICategoryBudgetRemainingCalculator budgetRemainingCalculator)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var transaction = new Transaction
        {
            Type = request.Type,
            Description = request.Description,
            Amount = request.Amount,
            RawCategory = request.RawCategory,
            ExpectedDate = request.ExpectedDate,
            ActualDate = request.ActualDate,
            ConfirmationDate = request.ConfirmationDate,
            InvoiceDueDate = request.InvoiceDueDate,
            Recurrence = request.Recurrence,
            CurrentInstallment = request.CurrentInstallment,
            TotalInstallments = request.TotalInstallments,
            AccountId = request.AccountId
        };

        // Stamps ScheduledAt/ConfirmedAt when the initial status warrants it (Card #14).
        transaction.TransitionTo(request.Status, now);

        await categoryResolver.ResolveAsync(transaction, cancellationToken);

        db.Transactions.Add(transaction);

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        var budgetRemaining = await budgetRemainingCalculator.CalculateAsync(transaction, includeTransaction: true, cancellationToken);

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
            transaction.NeedsConfirmation(DateOnly.FromDateTime(now.UtcDateTime)),
            budgetRemaining.RemainingBudget,
            budgetRemaining.RemainingBudgetPercentage);
    }
}
