using System.ComponentModel;
using FinancialMcp.Application.Categories.ListCategories;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class CategoryTools(IMediator mediator)
{
    [McpServerTool(Name = "list_categories"), Description(
        "Lista categorias-mãe e subcategorias em uso (parse de Categoria-mãe/Subcategoria).")]
    public Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new ListCategoriesQuery(), cancellationToken);
}
