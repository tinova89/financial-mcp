using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Transactions.DeleteTransaction;

/// <summary>
/// Removes (soft delete) a transaction. Destructive operation: Confirm must be
/// explicitly true, validated by ValidationBehavior (see CLAUDE.md > Mediator
/// Pattern > Destructive operations / What Claude Should Avoid). Returns the deleted
/// transaction's TransactionDto, with RemainingBudget/RemainingBudgetPercentage computed as
/// if it no longer counts toward its category's spend.
/// </summary>
public sealed record DeleteTransactionCommand(Guid TransactionId, bool Confirm)
    : IRequest<TransactionDto>, ITransactionalRequest;
