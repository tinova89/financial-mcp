using FinancialMcp.Application.BudgetGoals.CreateCategoryBudget;
using FinancialMcp.Application.Common.Services;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20.1 — end-to-end `RemainingBudgetPercentage` threshold coverage for
/// `create_transaction`, running the real <see cref="CategoryBudgetRemainingCalculator"/>
/// (no mock/stub) against a real <see cref="CreateCategoryBudgetCommandHandler"/> goal and a
/// real <see cref="CreateTransactionCommandHandler"/> transaction, over a
/// <see cref="SqliteInMemoryDatabase"/>.
///
/// <para>
/// <b>Important semantics correction (flagged, not silently applied):</b> the card this test
/// file implements assumed <c>RemainingBudgetPercentage</c> was a 0–100 "percent spent" value
/// (e.g. <c>150</c> for a 150%-of-budget overspend). The actual implementation
/// (<see cref="CategoryBudgetRemainingCalculator"/>, confirmed by <c>TransactionDto</c>'s own
/// doc comment) computes <c>RemainingBudget / GoalAmount</c> — the fraction of the budget
/// still <em>remaining</em>, on a 0–1 scale, never clamped (so it goes negative on overspend,
/// e.g. <c>-0.5</c> rather than <c>150</c>). This is a different field, with different
/// semantics, from `get_budget_status`'s `PercentUsed` (<c>ActualSpend / GoalAmount</c>), which
/// this card explicitly excludes. Every scenario below asserts the real value on the real
/// scale — see the user-facing summary for the corrected number table. No production code
/// was changed to make these pass.
/// </para>
/// </summary>
public class CreateTransactionRemainingBudgetThresholdTests
{
    private const int Year = 2026;
    private const int Month = 6;
    private static readonly DateOnly ReferenceDate = new(Year, Month, 1);

    private static async Task<BudgetGoalDto> CreateGoalAsync(ApplicationDbContext ctx, Guid categoryId, decimal goalAmount)
    {
        var handler = new CreateCategoryBudgetCommandHandler(ctx);
        var dto = await handler.Handle(
            new CreateCategoryBudgetCommand(categoryId, goalAmount, "BRL", BudgetPeriodType.OneTime, Year, Month),
            CancellationToken.None);
        await ctx.SaveChangesAsync();
        return dto;
    }

    private static async Task<TransactionDto> CreateConfirmedExpenseAsync(
        ApplicationDbContext ctx, Guid accountId, string categoryName, decimal amount)
    {
        var resolver = new TransactionCategoryResolver(ctx);
        var calculator = new CategoryBudgetRemainingCalculator(ctx);
        var handler = new CreateTransactionCommandHandler(ctx, resolver, calculator);

        var command = new CreateTransactionCommand(
            Type: TransactionType.Expense,
            Status: TransactionStatus.Confirmed,
            Description: "Despesa",
            Amount: -amount,
            RawCategory: categoryName,
            ExpectedDate: ReferenceDate,
            ActualDate: null,
            ConfirmationDate: ReferenceDate,
            InvoiceDueDate: null,
            Recurrence: RecurrenceType.None,
            CurrentInstallment: null,
            TotalInstallments: null,
            AccountId: accountId);

        var dto = await handler.Handle(command, CancellationToken.None);
        await ctx.SaveChangesAsync();
        return dto;
    }

    private static async Task<(Account Account, TransactionCategory Category)> SeedAccountAndCategoryAsync(
        SqliteInMemoryDatabase db, string categoryName = "Moradia")
    {
        var account = RevisionSeed.NewCheckingAccountBanckCode260();
        var category = new TransactionCategory { Name = categoryName };

        await using var seed = db.NewContext();
        seed.Accounts.Add(account);
        seed.TransactionCategories.Add(category);
        await seed.SaveChangesAsync();

        return (account, category);
    }

    [Fact]
    public async Task Scenario1_Spending_under_half_of_the_goal_leaves_more_than_half_of_the_budget_remaining()
    {
        using var db = new SqliteInMemoryDatabase();
        var (account, category) = await SeedAccountAndCategoryAsync(db);

        await using var ctx = db.NewContext();
        await CreateGoalAsync(ctx, category.Id, goalAmount: 100m);
        var dto = await CreateConfirmedExpenseAsync(ctx, account.Id, category.Name, amount: 30m);

        dto.RemainingBudget.Should().Be(100m - 30m);
        dto.RemainingBudgetPercentage.Should().Be(0.7m);
        dto.RemainingBudgetPercentage.Should().BeGreaterThan(0.5m);
    }

    [Fact]
    public async Task Scenario2_Spending_between_half_and_80_percent_leaves_between_20_and_50_percent_remaining()
    {
        using var db = new SqliteInMemoryDatabase();
        var (account, category) = await SeedAccountAndCategoryAsync(db);

        await using var ctx = db.NewContext();
        await CreateGoalAsync(ctx, category.Id, goalAmount: 100m);
        var dto = await CreateConfirmedExpenseAsync(ctx, account.Id, category.Name, amount: 65m);

        dto.RemainingBudget.Should().Be(100m - 65m);
        dto.RemainingBudgetPercentage.Should().Be(0.35m);
        dto.RemainingBudgetPercentage.Should().BeInRange(0.2m, 0.5m);
    }

    [Fact]
    public async Task Scenario3_Spending_over_80_percent_leaves_less_than_20_percent_remaining()
    {
        using var db = new SqliteInMemoryDatabase();
        var (account, category) = await SeedAccountAndCategoryAsync(db);

        await using var ctx = db.NewContext();
        await CreateGoalAsync(ctx, category.Id, goalAmount: 100m);
        var dto = await CreateConfirmedExpenseAsync(ctx, account.Id, category.Name, amount: 90m);

        dto.RemainingBudget.Should().Be(100m - 90m);
        dto.RemainingBudgetPercentage.Should().Be(0.1m);
        dto.RemainingBudgetPercentage.Should().BeLessThan(0.2m);
    }

    [Fact]
    public async Task Scenario4_A_goal_of_100_with_a_50_expense_leaves_exactly_half_the_budget_remaining()
    {
        using var db = new SqliteInMemoryDatabase();
        var (account, category) = await SeedAccountAndCategoryAsync(db);

        await using var ctx = db.NewContext();
        await CreateGoalAsync(ctx, category.Id, goalAmount: 100m);
        var dto = await CreateConfirmedExpenseAsync(ctx, account.Id, category.Name, amount: 50m);

        dto.RemainingBudget.Should().Be(50m);
        dto.RemainingBudgetPercentage.Should().Be(0.5m);
    }

    [Fact]
    public async Task Scenario5_Decimal_precision_end_to_end_with_no_double_conversion_anywhere_in_the_chain()
    {
        using var db = new SqliteInMemoryDatabase();
        var (account, category) = await SeedAccountAndCategoryAsync(db);

        await using var ctx = db.NewContext();
        await CreateGoalAsync(ctx, category.Id, goalAmount: 133.33m);
        var dto = await CreateConfirmedExpenseAsync(ctx, account.Id, category.Name, amount: 33.33m);

        dto.RemainingBudget.Should().Be(100.00m);
        // Computed via the same decimal-only arithmetic the production calculator uses
        // (RemainingBudget / GoalAmount, both decimal) — no double anywhere in the chain.
        dto.RemainingBudgetPercentage.Should().Be(100.00m / 133.33m);
    }

    [Fact]
    public async Task Scenario6_Overspending_past_the_goal_produces_a_negative_unclamped_remaining_budget_and_percentage()
    {
        using var db = new SqliteInMemoryDatabase();
        var (account, category) = await SeedAccountAndCategoryAsync(db);

        await using var ctx = db.NewContext();
        await CreateGoalAsync(ctx, category.Id, goalAmount: 100m);
        var dto = await CreateConfirmedExpenseAsync(ctx, account.Id, category.Name, amount: 150m);

        dto.RemainingBudget.Should().Be(-50m, "the calculator never floors RemainingBudget at zero");
        dto.RemainingBudgetPercentage.Should().Be(-0.5m, "the calculator never floors RemainingBudgetPercentage at zero");
    }

    [Fact]
    public async Task Scenario7_A_transaction_against_one_categorys_goal_does_not_affect_another_categorys_goal()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewCheckingAccountBanckCode260();
        var categoryA = new TransactionCategory { Name = "Moradia" };
        var categoryB = new TransactionCategory { Name = "Lazer" };

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.AddRange(categoryA, categoryB);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        await CreateGoalAsync(ctx, categoryA.Id, goalAmount: 100m);
        await CreateGoalAsync(ctx, categoryB.Id, goalAmount: 200m);

        var dtoA = await CreateConfirmedExpenseAsync(ctx, account.Id, categoryA.Name, amount: 50m);

        dtoA.RemainingBudget.Should().Be(50m);
        dtoA.RemainingBudgetPercentage.Should().Be(0.5m);

        // Category B was never touched by the write above. Read its status through the same
        // real calculator used by create_transaction — via a throwaway, unpersisted probe
        // transaction with includeTransaction: false, so it reflects only already-persisted
        // expenses (i.e. none) for category B, without creating a second real transaction
        // (the scenario only calls for one) and without going through get_budget_status.
        var probe = new Transaction
        {
            Type = TransactionType.Expense,
            Status = TransactionStatus.Confirmed,
            Description = "probe",
            Amount = 0m,
            AccountId = account.Id,
            Category = categoryB,
            CategoryId = categoryB.Id,
            ExpectedDate = ReferenceDate,
            ConfirmationDate = ReferenceDate,
            Recurrence = RecurrenceType.None,
        };

        var calculator = new CategoryBudgetRemainingCalculator(ctx);
        var remainingB = await calculator.CalculateAsync(probe, includeTransaction: false, CancellationToken.None);

        remainingB.RemainingBudget.Should().Be(200m, "category B's goal is untouched by category A's transaction");
        remainingB.RemainingBudgetPercentage.Should().Be(1.0m, "100% of category B's budget remains — nothing was spent against it");
    }
}
