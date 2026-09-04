using FinancialMcp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Covers CLAUDE.md > Testing Guidelines > "ActualSpend aggregation by
/// parent category vs. full subcategory".
/// </summary>
public class CategoryTests
{
    [Fact]
    public void Parse_should_split_parent_category_and_subcategory()
    {
        var category = Category.Parse("Moradia/Seguro");

        category.ParentCategory.Should().Be("Moradia");
        category.Subcategory.Should().Be("Seguro");
        category.HasSubcategory.Should().BeTrue();
    }

    [Fact]
    public void Parse_without_slash_should_have_null_subcategory()
    {
        var category = Category.Parse("Moradia");

        category.ParentCategory.Should().Be("Moradia");
        category.Subcategory.Should().BeNull();
        category.HasSubcategory.Should().BeFalse();
    }

    [Fact]
    public void Goal_with_only_parent_category_should_match_any_subcategory()
    {
        var housingGoal = Category.Parse("Moradia");
        var insuranceExpense = Category.Parse("Moradia/Seguro");
        var rentExpense = Category.Parse("Moradia/Aluguel");

        insuranceExpense.MatchesGoal(housingGoal).Should().BeTrue();
        rentExpense.MatchesGoal(housingGoal).Should().BeTrue();
    }

    [Fact]
    public void Goal_with_full_subcategory_should_require_exact_match()
    {
        var insuranceGoal = Category.Parse("Moradia/Seguro");
        var insuranceExpense = Category.Parse("Moradia/Seguro");
        var rentExpense = Category.Parse("Moradia/Aluguel");

        insuranceExpense.MatchesGoal(insuranceGoal).Should().BeTrue();
        rentExpense.MatchesGoal(insuranceGoal).Should().BeFalse();
    }
}
