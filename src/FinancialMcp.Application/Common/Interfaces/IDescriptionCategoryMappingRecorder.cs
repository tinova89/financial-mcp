namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>
/// Learns the description→category association for a transaction being created/updated,
/// so future transactions with the same description can be auto-suggested via the
/// `lookup_category` MCP tool. Record() schedules the work and returns immediately — it
/// must never add latency to the create/update transaction call it's invoked from.
/// </summary>
public interface IDescriptionCategoryMappingRecorder
{
    void Record(string description, Guid categoryId);
}
