using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Transactions.GetTransaction;

/// <summary>Detalhe de uma transação específica. Corresponde à tool MCP `get_transaction`.</summary>
public sealed record GetTransactionQuery(Guid TransactionId) : IRequest<TransactionDto>;
