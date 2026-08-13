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
        var targetMonthYear = new MonthYear(request.Year, request.Month);

        var budgetGoals = await db.BudgetGoals
            .AsNoTracking()
            .Where(m => m.Year == request.Year && m.Month == request.Month)
            .ToListAsync(cancellationToken);

        if (budgetGoals.Count == 0)
        {
            return [];
        }

        // Brings in a reasonable universe of candidates (Expense + Reconciled) and filters the
        // reference Mês_Ano in memory, since it depends on which date column to use based on
        // Account.Kind (a rule that isn't directly translatable to plain SQL without duplicating
        // logic — kept centralized in Transaction.GetReferenceMonthYear()). Include(Account) is
        // required since that method reads Account.Kind.
        var candidates = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category).ThenInclude(c => c.ParentCategory)
            .Where(t => t.Status == TransactionStatus.Reconciled && t.Type == TransactionType.Expense)
            .ToListAsync(cancellationToken);

        var monthExpenses = candidates
            .Where(t => t.GetReferenceMonthYear() == targetMonthYear)
            .ToList();

        var result = new List<BudgetStatusDto>(budgetGoals.Count);

        foreach (var goal in budgetGoals)
        {
            var goalCategory = goal.Category;

            var categoryExpenses = monthExpenses
                .Where(t => t.Category.MatchesGoal(goalCategory))
                .ToList();

            var actualSpent = categoryExpenses.Sum(t => Math.Abs(t.Amount));
            var remainingBudget = goal.BudgetAmount - actualSpent;
            decimal? utilizationPercentage = goal.BudgetAmount == 0m ? null : actualSpent / goal.BudgetAmount;

            var breakdown = categoryExpenses
                .GroupBy(t => t.Category.Subcategory)
                .Select(g => new SubcategoryBreakdownDto(g.Key, g.Sum(t => Math.Abs(t.Amount))))
                .OrderByDescending(d => d.ActualSpent)
                .ToList();

            result.Add(new BudgetStatusDto(
                goal.RawCategory,
                goal.BudgetAmount,
                actualSpent,
                remainingBudget,
                utilizationPercentage,
                breakdown));
        }

        return result;
    }
}
