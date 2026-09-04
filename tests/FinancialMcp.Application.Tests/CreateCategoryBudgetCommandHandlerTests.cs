using FinancialMcp.Application.BudgetGoals.CreateCategoryBudget;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `create_category_budget`. The handler only checks that the category exists;
/// the "must be a parent, not a subcategory" and "no duplicate goal for category/month"
/// rules are validator-only (<see cref="CreateCategoryBudgetCommandValidator"/>).
/// </summary>
public class CreateCategoryBudgetCommandHandlerTests
{
    #region Handler

    [Fact]
    public async Task Handler_throws_not_found_when_the_category_does_not_exist()
    {
        var categories = new List<TransactionCategory>().BuildMockDbSet();
        var db = Substitute.For<IApplicationDbContext>();
        db.TransactionCategories.Returns(categories);

        var handler = new CreateCategoryBudgetCommandHandler(db);
        var command = new CreateCategoryBudgetCommand(Guid.NewGuid(), 500m, "BRL", BudgetPeriodType.Monthly, 2026, 6);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handler_persists_the_budget_goal_with_the_correct_amount_period_year_and_month()
    {
        using var db = new SqliteInMemoryDatabase();
        var category = RevisionSeed.NewCategory("Moradia");

        await using (var seed = db.NewContext())
        {
            seed.TransactionCategories.Add(category);
            await seed.SaveChangesAsync();
        }

        Guid goalId;
        await using (var ctx = db.NewContext())
        {
            var handler = new CreateCategoryBudgetCommandHandler(ctx);
            var command = new CreateCategoryBudgetCommand(category.Id, 750m, "BRL", BudgetPeriodType.OneTime, 2026, 8);

            var dto = await handler.Handle(command, CancellationToken.None);
            await ctx.SaveChangesAsync();

            dto.CategoryId.Should().Be(category.Id);
            dto.CategoryName.Should().Be("Moradia");
            goalId = dto.Id;
        }

        await using var assert = db.NewContext();
        var stored = await assert.BudgetGoals.SingleAsync(g => g.Id == goalId);
        stored.RawCategoryId.Should().Be(category.Id);
        stored.GoalAmount.Should().Be(750m);
        stored.CurrencyCode.Should().Be("BRL");
        stored.Period.Should().Be(BudgetPeriodType.OneTime);
        stored.Year.Should().Be(2026);
        stored.Month.Should().Be(8);
    }

    #endregion

    #region Validator

    private static CreateCategoryBudgetCommandValidator BuildValidator(
        IReadOnlyList<TransactionCategory>? categories = null, IReadOnlyList<BudgetGoal>? goals = null)
    {
        // Build the mock DbSets before touching the substitute — BuildMockDbSet() configures
        // its own NSubstitute internally and would otherwise clobber the pending Returns() call.
        var categorySet = (categories ?? []).ToList().BuildMockDbSet();
        var goalSet = (goals ?? []).ToList().BuildMockDbSet();

        var db = Substitute.For<IApplicationDbContext>();
        db.TransactionCategories.Returns(categorySet);
        db.BudgetGoals.Returns(goalSet);

        return new CreateCategoryBudgetCommandValidator(db);
    }

    private static CreateCategoryBudgetCommand ValidCommand(Guid categoryId, int year = 2026, int month = 6) =>
        new(categoryId, 500m, "BRL", BudgetPeriodType.Monthly, year, month);

    [Fact]
    public async Task Validator_rejects_a_subcategory_as_the_target_category()
    {
        var parent = new TransactionCategory { Name = "Moradia" };
        var subcategory = new TransactionCategory { Name = "Aluguel", ParentCategoryId = parent.Id };

        var validator = BuildValidator(categories: [parent, subcategory]);
        var result = await validator.TestValidateAsync(ValidCommand(subcategory.Id));

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Validator_accepts_a_parent_category()
    {
        var parent = new TransactionCategory { Name = "Moradia" };

        var validator = BuildValidator(categories: [parent]);
        var result = await validator.TestValidateAsync(ValidCommand(parent.Id));

        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Validator_rejects_a_duplicate_goal_for_the_same_category_and_month()
    {
        var parent = new TransactionCategory { Name = "Moradia" };
        var existingGoal = RevisionSeed.NewBudgetGoal(parent, BudgetPeriodType.Monthly, 2026, 6, 300m);

        var validator = BuildValidator(categories: [parent], goals: [existingGoal]);
        var result = await validator.TestValidateAsync(ValidCommand(parent.Id, year: 2026, month: 6));

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task Validator_accepts_a_goal_for_the_same_category_in_a_different_month()
    {
        var parent = new TransactionCategory { Name = "Moradia" };
        var existingGoal = RevisionSeed.NewBudgetGoal(parent, BudgetPeriodType.Monthly, 2026, 6, 300m);

        var validator = BuildValidator(categories: [parent], goals: [existingGoal]);
        var result = await validator.TestValidateAsync(ValidCommand(parent.Id, year: 2026, month: 7));

        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    #endregion
}
