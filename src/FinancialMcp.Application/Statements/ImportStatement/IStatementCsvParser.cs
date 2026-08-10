using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Parser for the statement format (";" separator, dd/mm/yyyy dates, dot-decimal).
/// Implemented in FinancialMcp.Infrastructure (uses CsvHelper) and injected
/// here via abstraction, to keep the Application layer free of I/O dependencies.
/// </summary>
public interface IStatementCsvParser
{
    IReadOnlyList<Transaction> Parse(string csvContent, TransactionSource source, Guid? accountId, Guid? cardId, out IReadOnlyList<string> warnings);
}
