using FinancialMcp.Application.Common.Services;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #14 — Gasto_Real aggregation counts only <c>Confirmed</c> expenses
/// (see CLAUDE.md > Business Rules > Budget goals, item 1).
/// </summary>
public class ActualSpendCalculatorTests
{
    private static readonly MonthYear August2026 = new(2026, 8);

    private static Transaction Expense(TransactionStatus status, decimal amount, string parentCategory = "Moradia") => new()
    {
        Type = TransactionType.Expense,
        Status = status,
        Amount = amount,
        ReconciledDate = new DateOnly(2026, 8, 15),
        Account = new Account(),                                  // checking account (Kind = Debit)
        Category = new TransactionCategory { Name = parentCategory }
    };

    [Fact]
    public void Sums_only_confirmed_expenses_for_the_category_and_month()
    {
        var transactions = new[]
        {
            Expense(TransactionStatus.Confirmed, -100m),
            Expense(TransactionStatus.Confirmed, -50m),
            Expense(TransactionStatus.Scheduled, -999m),   // not confirmed → ignored
            Expense(TransactionStatus.Revision, -777m),    // not confirmed → ignored
        };

        var total = ActualSpendCalculator.SumForCategoryMonth(transactions, "Moradia", August2026);

        total.Should().Be(150m);
    }

    [Fact]
    public void Ignores_confirmed_expenses_in_a_different_month()
    {
        var julyExpense = Expense(TransactionStatus.Confirmed, -300m);
        julyExpense.ReconciledDate = new DateOnly(2026, 7, 31);

        var transactions = new[] { Expense(TransactionStatus.Confirmed, -120m), julyExpense };

        ActualSpendCalculator.SumForCategoryMonth(transactions, "Moradia", August2026).Should().Be(120m);
    }

    [Fact]
    public void Ignores_confirmed_expenses_in_a_different_parent_category()
    {
        var transactions = new[]
        {
            Expense(TransactionStatus.Confirmed, -80m, "Moradia"),
            Expense(TransactionStatus.Confirmed, -200m, "Lazer"),
        };

        ActualSpendCalculator.SumForCategoryMonth(transactions, "Moradia", August2026).Should().Be(80m);
    }
}
