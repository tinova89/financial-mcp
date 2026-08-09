using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Transactions.DeleteTransaction;

/// <summary>
/// Removes (soft delete) a transaction. Destructive operation: Confirm must be
/// explicitly true, validated by ValidationBehavior (see CLAUDE.md > Mediator
/// Pattern > Destructive operations / What Claude Should Avoid).
/// </summary>
public sealed record DeleteTransactionCommand(Guid TransactionId, bool Confirm)
    : IRequest, ITransactionalRequest;
