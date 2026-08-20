using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Learned mapping from a transaction's Description to the TransactionCategory it was last
/// categorized under (e.g. "Rede economia" → Mercado/Avulso). Populated automatically —
/// get-or-create/update — whenever a transaction is created or updated (see
/// IDescriptionCategoryMappingRecorder); there is no dedicated create/update MCP tool for
/// it. Consumed by the read-only `lookup_category` MCP tool.
/// </summary>
public sealed class DescriptionCategoryMapping : BaseEntity
{
    public string Description { get; set; } = default!;

    public Guid CategoryId { get; set; }
    public TransactionCategory Category { get; set; } = default!;
}
