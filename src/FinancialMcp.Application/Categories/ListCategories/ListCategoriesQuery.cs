using MediatR;

namespace FinancialMcp.Application.Categories.ListCategories;

/// <summary>Lists parent categories and subcategories in use. Corresponds to the MCP tool `list_categories`.</summary>
public sealed record ListCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

/// <param name="CategoryId">Id of the parent TransactionCategory row (not any of its subcategories).</param>
public sealed record CategoryDto(Guid CategoryId, string ParentCategory, IReadOnlyList<string> Subcategories);
