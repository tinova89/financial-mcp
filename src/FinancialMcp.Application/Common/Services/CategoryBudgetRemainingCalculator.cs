using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Common.Services;

/// <summary>See ICategoryBudgetRemainingCalculator.</summary>
public sealed class CategoryBudgetRemainingCalculator(IApplicationDbContext db) : ICategoryBudgetRemainingCalculator
{
    public async Task<CategoryBudgetRemaining> CalculateAsync(Transaction transaction, bool includeTransaction, CancellationToken cancellationToken)
    {
        var accountKind = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == transaction.AccountId)
            .Select(a => a.Kind)
            .FirstAsync(cancellationToken);

        var referenceMonthYear = transaction.GetReferenceMonthYear(accountKind);
        if (referenceMonthYear is null)
        {
            return CategoryBudgetRemaining.None;
        }

        var parentCategoryName = transaction.Category.ParentCategoryName;

        // Mirrors GetBudgetStatusQueryHandler: brings in every goal ever registered for the
        // category, since a Monthly goal can apply to a month after its own PeriodReference —
        // see BudgetGoal.ResolveEffective.
        var allGoals = await db.BudgetGoals.AsNoTracking().Include(g => g.RawCategory).ToListAsync(cancellationToken);
        var goalsForCategory = allGoals.Where(g => string.Equals(g.RawCategory.Name, parentCategoryName, StringComparison.OrdinalIgnoreCase));
        var effectiveGoal = BudgetGoal.ResolveEffective(goalsForCategory, referenceMonthYear.Value.Year, referenceMonthYear.Value.Month);

        if (effectiveGoal is null)
        {
            return CategoryBudgetRemaining.None;
        }

        // Excludes this transaction's own row — at this point in the MediatR pipeline
        // SaveChangesAsync hasn't run yet (see TransactionBehavior), so a DB query would only
        // see its stale, pre-write state. Its post-write contribution is added back below from
        // the in-memory transaction instead.
        var otherExpenses = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category).ThenInclude(c => c.ParentCategory)
            .Where(t => t.Id != transaction.Id && t.Status == TransactionStatus.Confirmed && t.Type == TransactionType.Expense)
            .ToListAsync(cancellationToken);

        var actualSpent = otherExpenses
            .Where(t => t.GetReferenceMonthYear() == referenceMonthYear
                        && string.Equals(t.Category.ParentCategoryName, parentCategoryName, StringComparison.OrdinalIgnoreCase))
            .Sum(t => Math.Abs(t.Amount));

        if (includeTransaction && transaction.IsEligibleForActualSpend)
        {
            actualSpent += Math.Abs(transaction.Amount);
        }

        var remainingBudget = effectiveGoal.GoalAmount - actualSpent;
        decimal? remainingBudgetPercentage = effectiveGoal.GoalAmount == 0m ? null : remainingBudget / effectiveGoal.GoalAmount;

        return new CategoryBudgetRemaining(remainingBudget, remainingBudgetPercentage);
    }
}
