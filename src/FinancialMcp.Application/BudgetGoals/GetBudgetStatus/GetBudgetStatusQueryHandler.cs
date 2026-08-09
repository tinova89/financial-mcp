using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

/// <summary>
/// Single handler for GetBudgetStatusQuery. Implements exactly the rules from
/// CLAUDE.md > Business Rules > Budget goals:
///  1. Only Status = Conciliado.
///  2. Only Tipo = Despesa (never Receita/Transferência/Pagamento).
///  3. Mês_Ano: "Data Conciliado" (checking account) / "Venc. Fatura" (credit card).
///  4. Gasto_Real / Saldo_Meta / % Utilizado.
///  5. Categories without a goal don't appear in the result.
/// </summary>
public sealed class GetBudgetStatusQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBudgetStatusQuery, IReadOnlyList<BudgetStatusDto>>
{
    public async Task<IReadOnlyList<BudgetStatusDto>> Handle(GetBudgetStatusQuery request, CancellationToken cancellationToken)
    {
        var targetMonthYear = new MesAno(request.Year, request.Month);

        var budgetGoals = await db.BudgetGoals
            .AsNoTracking()
            .Where(m => m.Ano == request.Year && m.Mes == request.Month)
            .ToListAsync(cancellationToken);

        if (budgetGoals.Count == 0)
        {
            return [];
        }

        // Brings in a reasonable universe of candidates (Despesa + Conciliado) and filters the
        // reference Mês_Ano in memory, since it depends on which date column to use
        // based on the Origem (a rule that isn't directly translatable to plain SQL
        // without duplicating logic — kept centralized in Transacao.ObterMesAnoReferencia()).
        var candidates = await db.Transactions
            .AsNoTracking()
            .Where(t => t.Status == StatusTransacao.Conciliado && t.Tipo == TipoTransacao.Despesa)
            .ToListAsync(cancellationToken);

        var monthExpenses = candidates
            .Where(t => t.ObterMesAnoReferencia() == targetMonthYear)
            .ToList();

        var result = new List<BudgetStatusDto>(budgetGoals.Count);

        foreach (var goal in budgetGoals)
        {
            var goalCategory = goal.Categoria;

            var categoryExpenses = monthExpenses
                .Where(t => t.Categoria.CasaComMeta(goalCategory))
                .ToList();

            var actualSpent = categoryExpenses.Sum(t => Math.Abs(t.Valor));
            var remainingBudget = goal.MetaValor - actualSpent;
            decimal? utilizationPercentage = goal.MetaValor == 0m ? null : actualSpent / goal.MetaValor;

            var breakdown = categoryExpenses
                .GroupBy(t => t.Categoria.Subcategoria)
                .Select(g => new SubcategoryBreakdownDto(g.Key, g.Sum(t => Math.Abs(t.Valor))))
                .OrderByDescending(d => d.ActualSpent)
                .ToList();

            result.Add(new BudgetStatusDto(
                goal.CategoriaBruta,
                goal.MetaValor,
                actualSpent,
                remainingBudget,
                utilizationPercentage,
                breakdown));
        }

        return result;
    }
}
