using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Insere uma nova transação (CC ou CD), respeitando os campos obrigatórios de
/// cada extrato. Corresponde à tool MCP `create_transaction` (ver CLAUDE.md > MCP).
/// </summary>
public sealed record CreateTransactionCommand(
    string Origem,            // "ContaCorrente" | "CartaoCredito"
    string Tipo,               // "Despesa" | "Receita" | "Transferencia" | "Pagamento"
    string Status,
    string Descricao,
    decimal Valor,
    string CategoriaBruta,     // "Categoria-mãe/Subcategoria"
    DateOnly DataPrevista,
    DateOnly? DataEfetiva,
    DateOnly? DataConciliado,
    DateOnly? VencimentoFatura,
    string? Repeticao,
    int? ParcelaAtual,
    int? ParcelaTotal,
    Guid? ContaId,
    Guid? CartaoId) : IRequest<TransactionDto>, ITransactionalRequest;
