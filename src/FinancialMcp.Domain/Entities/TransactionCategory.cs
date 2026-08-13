using FinancialApp.Model;
using FinancialMcp.Domain.Common;
using FinancialMcp.Domain.ValueObjects;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// A category — or subcategory, when <see cref="ParentCategory"/> is set — used by
/// transactions. Rows are only ever created as a side effect of resolving a Transaction's
/// RawCategory (get-or-create on create/import/update, see ITransactionCategoryResolver) —
/// there is no dedicated create/update MCP tool for categories; `list_categories` remains
/// the only read entry point (see CLAUDE.md > MCP).
/// </summary>
public sealed class TransactionCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public FinancialOperationKind Operation { get; set; }

    /// <summary>Null for a top-level (parent) category.</summary>
    public Guid? ParentCategoryId { get; set; }
    public TransactionCategory? ParentCategory { get; set; }

    /// <summary>The top-level category name — this category itself when it has no parent.</summary>
    public string ParentCategoryName => ParentCategory?.Name ?? Name;

    /// <summary>Non-null only when this row represents a subcategory (ParentCategory is set).</summary>
    public string? Subcategory => ParentCategory is null ? null : Name;

    /// <summary>
    /// Same matching rule as Category.MatchesGoal (see CLAUDE.md > Category and subcategory):
    /// a goal registered with only the parent category matches any subcategory under it; a
    /// goal registered with a full subcategory requires an exact match.
    /// </summary>
    public bool MatchesGoal(Category goalCategory)
    {
        if (!string.Equals(ParentCategoryName, goalCategory.ParentCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !goalCategory.HasSubcategory
            || string.Equals(Subcategory, goalCategory.Subcategory, StringComparison.OrdinalIgnoreCase);
    }
}
