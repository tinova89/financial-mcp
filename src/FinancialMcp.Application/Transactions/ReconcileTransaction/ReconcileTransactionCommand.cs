using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Transactions.ReconcileTransaction;

/// <summary>
/// Marca uma transação como Conciliado (CC) ou equivalente em CD. Corresponde à
/// tool MCP `reconcile_transaction`. Publica TransactionReconciledNotification
/// (ver CLAUDE.md > Padrão Mediator > Notifications).
/// </summary>
public sealed record ReconcileTransactionCommand(Guid TransactionId, DateOnly? DataConciliado = null)
    : IRequest, ITransactionalRequest;
