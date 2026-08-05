using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.ListTransactions;

/// <summary>
/// Handler único para ListTransactionsQuery. Regra de cálculo pura de categoria
/// (parse "Categoria-mãe/Subcategoria") delegada ao value object Categoria do domínio.
/// </summary>
public sealed class ListTransactionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(ListTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Transacoes.AsNoTracking().AsQueryable();

        if (request.Origem is not null)
        {
            query = query.Where(t => t.Origem == Enum.Parse<OrigemTransacao>(request.Origem));
        }

        if (request.Tipo is not null)
        {
            query = query.Where(t => t.Tipo == Enum.Parse<TipoTransacao>(request.Tipo));
        }

        if (request.Status is not null)
        {
            query = query.Where(t => t.Status == Enum.Parse<StatusTransacao>(request.Status));
        }

        if (request.ContaId is not null)
        {
            query = query.Where(t => t.ContaId == request.ContaId);
        }

        if (request.CartaoId is not null)
        {
            query = query.Where(t => t.CartaoId == request.CartaoId);
        }

        if (request.PeriodoInicio is not null)
        {
            query = query.Where(t => t.DataPrevista >= request.PeriodoInicio);
        }

        if (request.PeriodoFim is not null)
        {
            query = query.Where(t => t.DataPrevista <= request.PeriodoFim);
        }

        if (request.CategoriaMae is not null)
        {
            // EF.Functions.Like evita trazer tudo para memória antes de filtrar por categoria-mãe.
            query = query.Where(t => EF.Functions.Like(t.CategoriaBruta, $"{request.CategoriaMae}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var transacoes = await query
            .OrderByDescending(t => t.DataPrevista)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Filtros de Subcategoria/Ano/Mês dependem do value object Categoria e do
        // MesAno de referência calculado em memória (Data Conciliado x Venc. Fatura).
        var itensFiltrados = transacoes
            .Where(t => request.Subcategoria is null || t.Categoria.Subcategoria == request.Subcategoria)
            .Where(t => request.Ano is null || t.ObterMesAnoReferencia()?.Ano == request.Ano)
            .Where(t => request.Mes is null || t.ObterMesAnoReferencia()?.Mes == request.Mes)
            .Select(Map)
            .ToList();

        return new PagedResult<TransactionDto>(itensFiltrados, request.Page, request.PageSize, totalCount);
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
