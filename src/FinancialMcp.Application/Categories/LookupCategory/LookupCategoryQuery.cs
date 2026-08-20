using MediatR;

namespace FinancialMcp.Application.Categories.LookupCategory;

/// <summary>
/// Resolves a category from a transaction description via the learned description→category
/// mapping table (see IDescriptionCategoryMappingRecorder). Corresponds to the MCP tool
/// `lookup_category`. Null result means no mapping has been learned for this description yet.
/// </summary>
public sealed record LookupCategoryQuery(string Description) : IRequest<CategoryLookupResultDto?>;

public sealed record CategoryLookupResultDto(Guid CategoryId, string ParentCategory, string? Subcategory);
