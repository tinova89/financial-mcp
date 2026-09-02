using MediatR;

namespace FinancialMcp.Application.Transactions.ConfirmTransaction;

/// <summary>
/// Notification published after a transaction is confirmed. Consumed by decoupled handlers
/// (e.g. recalculating cached get_budget_status, notifying clients via SignalR) —
/// should never carry mandatory logic for the main business rule.
/// </summary>
public sealed record TransactionConfirmedNotification(Guid TransactionId) : INotification;
