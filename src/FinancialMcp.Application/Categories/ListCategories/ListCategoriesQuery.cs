using MediatR;

namespace FinancialMcp.Application.Categories.ListCategories;

/// <summary>Lists parent categories and subcategories in use. Corresponds to the MCP tool `list_categories`.</summary>
public sealed record ListCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public sealed record CategoryDto(string ParentCategory, IReadOnlyList<string> Subcategories);
