using System.ComponentModel;
using FinancialMcp.Application.Categories.ListCategories;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class CategoryTools(IMediator mediator)
{
    [McpServerTool(Name = "list_categories"), Description(
        """
        Lists every distinct category currently in use across all transactions, grouped by
        parent category with its subcategories.

        ## Parameters
        None — this tool takes no filters and always returns the full set in use.

        ## Behavior
        - Read-only.
        - Categories are derived directly from transaction data — parsed from each
          transaction's raw `Categoria-mãe/Subcategoria` string (split on `/`), not from a
          separate managed category table. A category only appears here if at least one
          transaction currently uses it.
        - Grouped by parent category, each with the distinct list of subcategories seen
          under it (a parent with no subcategory rows returns an empty subcategory list).
        - Both lists are ordered alphabetically.
        - This is purely informational — it does not imply a budget goal exists for any of
          these categories (see `get_budget_status` for that).

        ## Example
        ```json
        {}
        ```

        ## Returns
        A list of `CategoryDto` (parentCategory, subcategories).
        """)]
    public Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new ListCategoriesQuery(), cancellationToken);
}
