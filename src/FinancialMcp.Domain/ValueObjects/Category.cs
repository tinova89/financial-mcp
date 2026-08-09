namespace FinancialMcp.Domain.ValueObjects;

/// <summary>
/// Value object for "Categoria-mãe/Subcategoria". Centralizes the parsing used by
/// every MCP tool that aggregates by category (see CLAUDE.md > Code Conventions >
/// Category/Subcategory). Single source of truth for the "/" split.
/// </summary>
public sealed record Category
{
    private const char Separator = '/';

    public string ParentCategory { get; }

    /// <summary>Null when the original category had no subcategory.</summary>
    public string? Subcategory { get; }

    /// <summary>Full original value, exactly as it came from the statement.</summary>
    public string FullValue { get; }

    private Category(string parentCategory, string? subcategory, string fullValue)
    {
        ParentCategory = parentCategory;
        Subcategory = subcategory;
        FullValue = fullValue;
    }

    public static Category Parse(string rawValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);

        var parts = rawValue.Split(Separator, 2, StringSplitOptions.TrimEntries);

        return parts.Length == 2
            ? new Category(parts[0], parts[1], rawValue)
            : new Category(parts[0], null, rawValue);
    }

    /// <summary>True when the goal was registered with a full subcategory (exact match).</summary>
    public bool HasSubcategory => Subcategory is not null;

    /// <summary>
    /// Compares this category (from a transaction) against a budget goal's category.
    /// A goal with only a parent category (e.g. "Moradia") matches any subcategory;
    /// a goal with a full subcategory (e.g. "Moradia/Seguro") requires an exact match.
    /// </summary>
    public bool MatchesGoal(Category goalCategory)
    {
        if (!string.Equals(ParentCategory, goalCategory.ParentCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !goalCategory.HasSubcategory
            || string.Equals(Subcategory, goalCategory.Subcategory, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => FullValue;
}
