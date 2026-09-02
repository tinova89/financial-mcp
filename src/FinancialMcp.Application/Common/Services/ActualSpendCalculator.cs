using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.ValueObjects;

namespace FinancialMcp.Application.Common.Services;

/// <summary>
/// Pure Gasto_Real aggregation rule (see CLAUDE.md > Business Rules > Budget goals, item 5),
/// extracted from the handlers so it can be unit-tested in isolation. A transaction counts
/// only when it is <see cref="Transaction.IsEligibleForActualSpend"/> — i.e.
/// <c>Status = Confirmed</c> and <c>Type = Expense</c> (Card #14) — its reference month
/// matches, and its parent category matches.
/// </summary>
public static class ActualSpendCalculator
{
    /// <summary>
    /// Sums the absolute amount of every transaction that counts toward Gasto_Real for
    /// <paramref name="parentCategoryName"/> in <paramref name="month"/>. Each transaction's
    /// <c>Account</c> and <c>Category</c> (with <c>ParentCategory</c>) must be loaded.
    /// </summary>
    public static decimal SumForCategoryMonth(
        IEnumerable<Transaction> transactions, string parentCategoryName, MonthYear month) =>
        transactions
            .Where(t => t.IsEligibleForActualSpend
                        && t.GetReferenceMonthYear() == month
                        && string.Equals(t.Category.ParentCategoryName, parentCategoryName, StringComparison.OrdinalIgnoreCase))
            .Sum(t => Math.Abs(t.Amount));
}
