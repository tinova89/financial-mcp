using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Transactions.ReconcileTransaction;

/// <summary>
/// Marks a transaction as Conciliado (checking account) or the equivalent for credit card.
/// Corresponds to the MCP tool `reconcile_transaction`. Publishes TransactionReconciledNotification
/// (see CLAUDE.md > Mediator Pattern > Notifications).
/// </summary>
public sealed record ReconcileTransactionCommand(Guid TransactionId, DateOnly? ReconciledDate = null)
    : IRequest, ITransactionalRequest;
