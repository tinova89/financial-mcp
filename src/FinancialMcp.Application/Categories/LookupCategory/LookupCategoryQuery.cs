using MediatR;

namespace FinancialMcp.Application.Categories.LookupCategory;

/// <summary>
/// Lists every category that currently carries an Instruction free-text hint. Corresponds
/// to the MCP tool `lookup_category`. Instruction is set via `update_category_instruction`
/// — it's no longer auto-learned from transaction descriptions.
/// </summary>
public sealed record LookupCategoryQuery : IRequest<IReadOnlyList<CategoryInstructionDto>>;

public sealed record CategoryInstructionDto(Guid CategoryId, string ParentCategory, string? Subcategory, string Instruction);
