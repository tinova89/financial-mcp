using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

/// <summary>
/// Altera campos de uma transação existente (status, categoria, valor, data).
/// Corresponde à tool MCP `update_transaction`. Campos nulos são ignorados (patch parcial).
/// </summary>
public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    string? Status = null,
    string? CategoriaBruta = null,
    decimal? Valor = null,
    DateOnly? DataPrevista = null,
    DateOnly? DataEfetiva = null,
    DateOnly? DataConciliado = null,
    DateOnly? VencimentoFatura = null) : IRequest<TransactionDto>, ITransactionalRequest;
