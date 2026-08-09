using MediatR;

namespace FinancialMcp.Application.Transactions.ReconcileTransaction;

/// <summary>
/// Notification published after reconciliation. Consumed by decoupled handlers
/// (e.g. recalculating cached get_budget_status, notifying clients via SignalR) —
/// should never carry mandatory logic for the main business rule.
/// </summary>
public sealed record TransactionReconciledNotification(Guid TransactionId) : INotification;
