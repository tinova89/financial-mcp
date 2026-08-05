using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

/// <summary>
/// Handler único para GetBudgetStatusQuery. Implementa exatamente as regras de
/// CLAUDE.md > Regras de Negócio > Metas de orçamento:
///  1. Apenas Status = Conciliado.
///  2. Apenas Tipo = Despesa (nunca Receita/Transferência/Pagamento).
///  3. Mês_Ano: "Data Conciliado" (CC) / "Venc. Fatura" (CD).
///  4. Gasto_Real / Saldo_Meta / % Utilizado.
///  5. Categorias sem meta não entram no resultado.
/// </summary>
public sealed class GetBudgetStatusQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBudgetStatusQuery, IReadOnlyList<BudgetStatusDto>>
{
    public async Task<IReadOnlyList<BudgetStatusDto>> Handle(GetBudgetStatusQuery request, CancellationToken cancellationToken)
    {
        var mesAnoAlvo = new MesAno(request.Ano, request.Mes);

        var metas = await db.MetasOrcamento
            .AsNoTracking()
            .Where(m => m.Ano == request.Ano && m.Mes == request.Mes)
            .ToListAsync(cancellationToken);

        if (metas.Count == 0)
        {
            return [];
        }

        // Traz um universo razoável de candidatas (Despesa + Conciliado) e filtra o
        // Mês_Ano de referência em memória, pois ele depende de qual coluna de data
        // usar conforme a Origem (regra que não é diretamente traduzível para SQL simples
        // sem duplicar lógica — mantida centralizada em Transacao.ObterMesAnoReferencia()).
        var candidatas = await db.Transacoes
            .AsNoTracking()
            .Where(t => t.Status == StatusTransacao.Conciliado && t.Tipo == TipoTransacao.Despesa)
            .ToListAsync(cancellationToken);

        var despesasDoMes = candidatas
            .Where(t => t.ObterMesAnoReferencia() == mesAnoAlvo)
            .ToList();

        var resultado = new List<BudgetStatusDto>(metas.Count);

        foreach (var meta in metas)
        {
            var categoriaMeta = meta.Categoria;

            var despesasDaCategoria = despesasDoMes
                .Where(t => t.Categoria.CasaComMeta(categoriaMeta))
                .ToList();

            var gastoReal = despesasDaCategoria.Sum(t => Math.Abs(t.Valor));
            var saldoMeta = meta.MetaValor - gastoReal;
            decimal? percentualUtilizado = meta.MetaValor == 0m ? null : gastoReal / meta.MetaValor;

            var detalhamento = despesasDaCategoria
                .GroupBy(t => t.Categoria.Subcategoria)
                .Select(g => new SubcategoriaBreakdownDto(g.Key, g.Sum(t => Math.Abs(t.Valor))))
                .OrderByDescending(d => d.GastoReal)
                .ToList();

            resultado.Add(new BudgetStatusDto(
                meta.CategoriaBruta,
                meta.MetaValor,
                gastoReal,
                saldoMeta,
                percentualUtilizado,
                detalhamento));
        }

        return resultado;
    }
}
