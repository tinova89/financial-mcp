using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Common.Services;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

/// <summary>
/// Single handler for GetBudgetStatusQuery. Implements exactly the rules from
/// CLAUDE.md > Business Rules > Budget goals:
///  1. Only Status = Confirmed (Card #14; formerly Conciliado).
///  2. Only Type = Expense (never Income/Transfer/Payment).
///  3. Reference month/year: ConfirmationDate (checking account) / InvoiceDueDate (credit card).
///  4. ActualSpend / RemainingBudget / PercentUsed.
///  5. Categories without a goal don't appear in the result.
///  6. Per category, the goal in effect for the requested month is picked via
///     BudgetGoal.ResolveEffective (exact OneTime match, else the latest applicable Monthly goal).
/// </summary>
public sealed class GetBudgetStatusQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBudgetStatusQuery, IReadOnlyList<BudgetStatusDto>>
{
    public async Task<IReadOnlyList<BudgetStatusDto>> Handle(GetBudgetStatusQuery request, CancellationToken cancellationToken)
    {
        var targetMonthYear = new MonthYear(request.Year, request.Month);

        // Brings in every goal ever registered per category, since a Monthly goal can apply to
        // a month after its own PeriodReference — see BudgetGoal.ResolveEffective.
        var allGoals = await db.BudgetGoals
            .AsNoTracking()
            .Include(m => m.RawCategory)
            .ToListAsync(cancellationToken);

        var budgetGoals = allGoals
            .GroupBy(m => m.RawCategoryId)
            .Select(g => BudgetGoal.ResolveEffective(g, request.Year, request.Month))
            .Where(g => g is not null)
            .Select(g => g!)
            .ToList();

        if (budgetGoals.Count == 0)
        {
            return [];
        }

        // Brings in a reasonable universe of candidates (Expense + Confirmed) and filters the
        // reference month/year in memory, since it depends on which date column to use based on
        // Account.Kind (a rule that isn't directly translatable to plain SQL without duplicating
        // logic — kept centralized in Transaction.GetReferenceMonthYear()). Include(Account) is
        // required since that method reads Account.Kind.
        var candidates = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category).ThenInclude(c => c.ParentCategory)
            .Where(t => t.Status == TransactionStatus.Confirmed && t.Type == TransactionType.Expense)
            .ToListAsync(cancellationToken);

        var monthExpenses = candidates
            .Where(t => t.GetReferenceMonthYear() == targetMonthYear)
            .ToList();

        var result = new List<BudgetStatusDto>(budgetGoals.Count);

        foreach (var goal in budgetGoals)
        {
            // BudgetGoal.RawCategory is always a parent category (see BudgetGoal doc comment),
            // so every subcategory under it counts toward ActualSpend — no exact-subcategory match.
            var categoryExpenses = monthExpenses
                .Where(t => string.Equals(t.Category.ParentCategoryName, goal.RawCategory.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var actualSpend = ActualSpendCalculator.SumForCategoryMonth(
                categoryExpenses, goal.RawCategory.Name, targetMonthYear);
            var remainingBudget = goal.GoalAmount - actualSpend;
            decimal? percentUsed = goal.GoalAmount == 0m ? null : actualSpend / goal.GoalAmount;

            var breakdown = categoryExpenses
                .GroupBy(t => t.Category.Subcategory)
                .Select(g => new SubcategoryBreakdownDto(g.Key, g.Sum(t => Math.Abs(t.Amount))))
                .OrderByDescending(d => d.ActualSpend)
                .ToList();

            result.Add(new BudgetStatusDto(
                goal.RawCategory.Name,
                goal.GoalAmount,
                actualSpend,
                remainingBudget,
                percentUsed,
                breakdown));
        }

        return result;
    }
}
