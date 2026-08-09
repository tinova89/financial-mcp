using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Transactions.GetTransaction;

/// <summary>Detail of a specific transaction. Corresponds to the MCP tool `get_transaction`.</summary>
public sealed record GetTransactionQuery(Guid TransactionId) : IRequest<TransactionDto>;
