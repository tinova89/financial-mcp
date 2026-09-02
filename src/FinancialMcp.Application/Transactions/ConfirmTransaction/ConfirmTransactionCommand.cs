using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Transactions.ConfirmTransaction;

/// <summary>
/// Marks a transaction as Confirmed (checking account or the equivalent for credit card).
/// Corresponds to the MCP tool `confirm_transaction`. Publishes TransactionConfirmedNotification
/// (see CLAUDE.md > Mediator Pattern > Notifications).
/// </summary>
public sealed record ConfirmTransactionCommand(Guid TransactionId, DateOnly? ConfirmedDate = null)
    : IRequest, ITransactionalRequest;
