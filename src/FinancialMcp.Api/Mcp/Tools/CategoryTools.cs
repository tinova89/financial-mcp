using System.ComponentModel;
using FinancialMcp.Application.Categories.ListCategories;
using FinancialMcp.Application.Categories.LookupCategory;
using FinancialMcp.Application.Categories.UpdateCategoryInstruction;
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
        A list of `CategoryDto` (categoryId, parentCategory, subcategories) — categoryId is
        the parent category's own id (not any of its subcategories').
        """)]
    public Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new ListCategoriesQuery(), cancellationToken);

    [McpServerTool(Name = "lookup_category"), Description(
        """
        Lists every category that currently carries an Instruction free-text hint (e.g.
        category Mercado/Avulso might carry the Instruction "Rede economia, Extra hiper").

        ## Parameters
        None — this tool takes no filters and always returns every category that has an
        Instruction set.

        ## Behavior
        - Read-only.
        - Instruction has no automatic learning anymore — it's only ever set via
          `update_category_instruction`. A category with no Instruction set is omitted here.
        - Useful before calling `create_transaction`/`update_transaction`, to check which
          category's Instruction hints match the transaction's description instead of
          guessing RawCategory from scratch.

        ## Example
        ```json
        {}
        ```

        ## Returns
        A list of `CategoryInstructionDto` (categoryId, parentCategory, subcategory,
        instruction).
        """)]
    public Task<IReadOnlyList<CategoryInstructionDto>> LookupCategoryAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new LookupCategoryQuery(), cancellationToken);

    [McpServerTool(Name = "update_category_instruction"), Description(
        """
        Sets a category's Instruction free-text hint, used by `lookup_category` to help
        match future transaction descriptions to this category.

        ## Parameters
        - `categoryId` (guid, required): id of the category (parent or subcategory) to
          update — from `list_categories` or `lookup_category`.
        - `instruction` (string, required, max 2000 chars): the free-text hint to store,
          e.g. "Rede economia, Extra hiper, Carrefour". Overwrites any previous value.

        ## Behavior
        - Write. Fails if `categoryId` doesn't match an existing category.
        - This is the only way Instruction gets written — it's no longer learned
          automatically from transaction descriptions on create/update.

        ## Example
        ```json
        { "categoryId": "b3f1...", "instruction": "Rede economia, Extra hiper" }
        ```

        ## Returns
        The updated `CategoryInstructionDto` (categoryId, parentCategory, subcategory,
        instruction).
        """)]
    public Task<CategoryInstructionDto> UpdateCategoryInstructionAsync(
        [Description("Id of the category (parent or subcategory) to update.")] Guid categoryId,
        [Description("Free-text hint to store for this category, e.g. \"Rede economia, Extra hiper\".")] string instruction,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new UpdateCategoryInstructionCommand(categoryId, instruction), cancellationToken);
}
