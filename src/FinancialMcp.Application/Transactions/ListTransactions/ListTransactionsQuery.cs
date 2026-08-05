using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Transactions.ListTransactions;

/// <summary>
/// Lista transações de CC e/ou CD com filtros. Corresponde à tool MCP
/// `list_transactions` (ver CLAUDE.md > MCP). Todos os filtros são opcionais.
/// </summary>
public sealed record ListTransactionsQuery(
    string? Origem = null,
    string? Tipo = null,
    string? Status = null,
    string? CategoriaMae = null,
    string? Subcategoria = null,
    Guid? ContaId = null,
    Guid? CartaoId = null,
    DateOnly? PeriodoInicio = null,
    DateOnly? PeriodoFim = null,
    int? Ano = null,
    int? Mes = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<TransactionDto>>;

/// <summary>Resultado paginado — estratégia de paginação padrão para list_transactions em bases grandes.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
