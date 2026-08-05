using MediatR;

namespace FinancialMcp.Application.Categories.ListCategories;

/// <summary>Lista categorias-mãe e subcategorias em uso. Corresponde à tool MCP `list_categories`.</summary>
public sealed record ListCategoriesQuery : IRequest<IReadOnlyList<CategoriaDto>>;

public sealed record CategoriaDto(string CategoriaMae, IReadOnlyList<string> Subcategorias);
