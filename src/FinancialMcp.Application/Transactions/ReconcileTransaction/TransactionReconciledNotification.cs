using MediatR;

namespace FinancialMcp.Application.Transactions.ReconcileTransaction;

/// <summary>
/// Notification publicada após a conciliação. Consumida por handlers desacoplados
/// (ex.: recalcular get_budget_status em cache, notificar clientes via SignalR) —
/// nunca deve carregar lógica obrigatória da regra de negócio principal.
/// </summary>
public sealed record TransactionReconciledNotification(Guid TransactionId) : INotification;
