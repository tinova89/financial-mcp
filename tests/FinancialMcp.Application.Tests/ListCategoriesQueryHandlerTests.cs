using FinancialMcp.Application.Categories.ListCategories;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `list_categories` groups subcategories under their parent and is purely
/// informational: it never implies a budget goal exists for a category.
/// </summary>
public class ListCategoriesQueryHandlerTests
{
    private static IApplicationDbContext DbWith(params TransactionCategory[] categories)
    {
        // Build the mock DbSet before touching the substitute — BuildMockDbSet() configures
        // its own NSubstitute internally and would otherwise clobber the pending Returns() call.
        var set = categories.ToList().BuildMockDbSet();
        var db = Substitute.For<IApplicationDbContext>();
        db.TransactionCategories.Returns(set);
        return db;
    }

    [Fact]
    public async Task Groups_subcategories_under_their_parent_ordered_by_name()
    {
        var parent = new TransactionCategory { Name = "Moradia" };
        var sub1 = new TransactionCategory { Name = "Seguro", ParentCategoryId = parent.Id };
        var sub2 = new TransactionCategory { Name = "Aluguel", ParentCategoryId = parent.Id };
        var sub3 = new TransactionCategory { Name = "Condominio", ParentCategoryId = parent.Id };

        var db = DbWith(parent, sub1, sub2, sub3);

        var result = await new ListCategoriesQueryHandler(db).Handle(new ListCategoriesQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.ParentCategory.Should().Be("Moradia");
        dto.Subcategories.Should().Equal("Aluguel", "Condominio", "Seguro");
    }

    [Fact]
    public async Task Returns_a_parent_with_no_subcategories_as_an_empty_list_not_an_error()
    {
        var parent = new TransactionCategory { Name = "Lazer" };
        var db = DbWith(parent);

        var result = await new ListCategoriesQueryHandler(db).Handle(new ListCategoriesQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.Subcategories.Should().NotBeNull();
        dto.Subcategories.Should().BeEmpty();
    }

    [Fact]
    public async Task Orders_parents_by_name()
    {
        var moradia = new TransactionCategory { Name = "Moradia" };
        var alimentacao = new TransactionCategory { Name = "Alimentação" };
        var zeladoria = new TransactionCategory { Name = "Zeladoria" };

        var db = DbWith(moradia, alimentacao, zeladoria);

        var result = await new ListCategoriesQueryHandler(db).Handle(new ListCategoriesQuery(), CancellationToken.None);

        result.Select(c => c.ParentCategory).Should().Equal("Alimentação", "Moradia", "Zeladoria");
    }

    [Fact]
    public async Task Response_dto_carries_no_budget_information()
    {
        // Locks in that list_categories is purely informational and never implies a budget
        // goal exists — CategoryDto has no goal-related field at all, and the handler never
        // touches BudgetGoals.
        var properties = typeof(CategoryDto).GetProperties().Select(p => p.Name);

        properties.Should().BeEquivalentTo("CategoryId", "ParentCategory", "Subcategories");
    }
}
