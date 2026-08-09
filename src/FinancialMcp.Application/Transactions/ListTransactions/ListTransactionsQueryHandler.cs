using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.ListTransactions;

/// <summary>
/// Single handler for ListTransactionsQuery. Pure category calculation rule
/// (parsing "Categoria-mãe/Subcategoria") delegated to the domain's Categoria value object.
/// </summary>
public sealed class ListTransactionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(ListTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Transactions.AsNoTracking().AsQueryable();

        if (request.Source is not null)
        {
            query = query.Where(t => t.Origem == Enum.Parse<OrigemTransacao>(request.Source));
        }

        if (request.Type is not null)
        {
            query = query.Where(t => t.Tipo == Enum.Parse<TipoTransacao>(request.Type));
        }

        if (request.Status is not null)
        {
            query = query.Where(t => t.Status == Enum.Parse<StatusTransacao>(request.Status));
        }

        if (request.AccountId is not null)
        {
            query = query.Where(t => t.ContaId == request.AccountId);
        }

        if (request.CardId is not null)
        {
            query = query.Where(t => t.CartaoId == request.CardId);
        }

        if (request.PeriodStart is not null)
        {
            query = query.Where(t => t.DataPrevista >= request.PeriodStart);
        }

        if (request.PeriodEnd is not null)
        {
            query = query.Where(t => t.DataPrevista <= request.PeriodEnd);
        }

        if (request.ParentCategory is not null)
        {
            // EF.Functions.Like avoids bringing everything into memory before filtering by parent category.
            query = query.Where(t => EF.Functions.Like(t.CategoriaBruta, $"{request.ParentCategory}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .OrderByDescending(t => t.DataPrevista)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Subcategory/Year/Month filters depend on the Categoria value object and the
        // reference MesAno calculated in memory (Data Conciliado vs. Venc. Fatura).
        var filteredItems = transactions
            .Where(t => request.Subcategory is null || t.Categoria.Subcategoria == request.Subcategory)
            .Where(t => request.Year is null || t.ObterMesAnoReferencia()?.Ano == request.Year)
            .Where(t => request.Month is null || t.ObterMesAnoReferencia()?.Mes == request.Month)
            .Select(Map)
            .ToList();

        return new PagedResult<TransactionDto>(filteredItems, request.Page, request.PageSize, totalCount);
    }

    private static TransactionDto Map(Transacao t) => new(
        t.Id,
        t.Origem.ToString(),
        t.Tipo.ToString(),
        t.Status.ToString(),
        t.Descricao,
        t.Valor,
        t.CategoriaBruta,
        t.DataPrevista,
        t.DataEfetiva,
        t.DataConciliado,
        t.VencimentoFatura,
        t.Repeticao.ToString(),
        t.ParcelaAtual,
        t.ParcelaTotal,
        t.ContaId,
        t.CartaoId);
}
