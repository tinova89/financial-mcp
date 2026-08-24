using FinancialMcp.Application.Categories.LookupCategory;
using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Categories.UpdateCategoryInstruction;

/// <summary>
/// Sets a category's Instruction free-text hint (e.g. "Rede economia, Extra hiper"),
/// consumed by the `lookup_category` MCP tool. Corresponds to the MCP tool
/// `update_category_instruction` — the only way Instruction gets written now that it's no
/// longer auto-learned from transaction descriptions.
/// </summary>
public sealed record UpdateCategoryInstructionCommand(Guid CategoryId, string Instruction)
    : IRequest<CategoryInstructionDto>, ITransactionalRequest;
