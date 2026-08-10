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
public sealed class CreateTransactionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = new Transaction
        {
            Source = request.Source,
            Type = request.Type,
            Status = request.Status,
            Description = request.Description,
            Amount = request.Amount,
            RawCategory = request.RawCategory,
            ExpectedDate = request.ExpectedDate,
            ActualDate = request.ActualDate,
            ReconciledDate = request.ReconciledDate,
            InvoiceDueDate = request.InvoiceDueDate,
            Recurrence = request.Recurrence,
            CurrentInstallment = request.CurrentInstallment,
            TotalInstallments = request.TotalInstallments,
            AccountId = request.AccountId,
            CardId = request.CardId
        };

        db.Transactions.Add(transaction);

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new TransactionDto(
            transaction.Id,
            transaction.Source.ToString(),
            transaction.Type.ToString(),
            transaction.Status.ToString(),
            transaction.Description,
            transaction.Amount,
            transaction.RawCategory,
            transaction.ExpectedDate,
            transaction.ActualDate,
            transaction.ReconciledDate,
            transaction.InvoiceDueDate,
            transaction.Recurrence.ToString(),
            transaction.CurrentInstallment,
            transaction.TotalInstallments,
            transaction.AccountId,
            transaction.CardId);
    }
}
