using System.ComponentModel;
using FinancialMcp.Application.Categories.ListCategories;
using FinancialMcp.Application.Categories.LookupCategory;
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
        - Categories are read from the persisted category table, but that table has no
          create/update tool of its own — rows are only ever registered as a side effect of
          creating/importing/updating a transaction (its raw `Categoria-mãe/Subcategoria`
          string is split on `/` and get-or-created at that point). A category only appears
          here if at least one transaction has used it.
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

    [McpServerTool(Name = "lookup_category"), Description(
        """
        Resolves a category from a transaction description, using the description→category
        mapping table learned from past transactions (e.g. "Rede economia" → Mercado/Avulso).

        ## Parameters
        - `description` (string, required): the transaction description to look up — matched
          case-insensitively against past descriptions.

        ## Behavior
        - Read-only.
        - The mapping table has no create/update tool of its own — a row is learned
          automatically every time `create_transaction` or `update_transaction` resolves a
          category for a transaction with that exact description (most recent categorization
          wins). A description that has never been seen on a transaction returns null.
        - Useful before calling `create_transaction`/`update_transaction`, to reuse the
          category a similar past transaction was already filed under instead of guessing
          RawCategory from scratch.

        ## Example
        ```json
        { "description": "Rede economia" }
        ```

        ## Returns
        `CategoryLookupResultDto` (categoryId, parentCategory, subcategory) if a mapping
        exists for this description, otherwise null.
        """)]
    public Task<CategoryLookupResultDto?> LookupCategoryAsync(
        [Description("Transaction description to resolve a category for.")] string description,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new LookupCategoryQuery(description), cancellationToken);
}
