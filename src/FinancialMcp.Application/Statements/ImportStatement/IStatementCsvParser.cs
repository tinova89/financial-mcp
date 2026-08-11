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
    /// <summary>
    /// accountId identifies the destination for both sources: the checking account itself for
    /// CheckingAccount rows, or the CreditCard's own id (it's an Account row via EF Core TPH) for
    /// CreditCard rows.
    /// </summary>
    IReadOnlyList<Transaction> Parse(string csvContent, TransactionSource source, Guid? accountId, out IReadOnlyList<string> warnings);
}
