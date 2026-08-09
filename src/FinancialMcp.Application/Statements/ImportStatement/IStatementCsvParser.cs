using FinancialMcp.Domain.Entities;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Parser for the statement format (";" separator, dd/mm/yyyy dates, dot-decimal).
/// Implemented in FinancialMcp.Infrastructure (uses CsvHelper) and injected
/// here via abstraction, to keep the Application layer free of I/O dependencies.
/// </summary>
public interface IStatementCsvParser
{
    IReadOnlyList<Transaction> Parse(string csvContent, string source, Guid? accountId, Guid? cardId, out IReadOnlyList<string> warnings);
}
