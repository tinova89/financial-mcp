using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

/// <summary>
/// Updates fields of an existing transaction (status, category, amount, date).
/// Corresponds to the MCP tool `update_transaction`. Null fields are ignored (partial patch).
/// </summary>
public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    string? Status = null,
    string? RawCategory = null,
    decimal? Amount = null,
    DateOnly? ExpectedDate = null,
    DateOnly? ActualDate = null,
    DateOnly? ReconciledDate = null,
    DateOnly? InvoiceDueDate = null) : IRequest<TransactionDto>, ITransactionalRequest;
