using FinancialMcp.Domain.Entities;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Parser for the statement format (";" separator, dd/mm/yyyy dates, dot-decimal).
/// Implemented in FinancialMcp.Infrastructure (uses CsvHelper) and injected
/// here via abstraction, to keep the Application layer free of I/O dependencies.
/// </summary>
public interface IStatementCsvParser
{
    /// <summary>
    /// accountId identifies the destination — the checking account itself, or a CreditCard's
    /// own id (it's an Account row via EF Core TPH) — for every parsed row. isCreditCard tells
    /// the parser which CSV columns to expect (Data Conciliado vs. Venc. Fatura/Repetição/
    /// Parcela), determined by the caller from the referenced account's actual Account.Kind.
    /// </summary>
    IReadOnlyList<Transaction> Parse(string csvContent, bool isCreditCard, Guid accountId, out IReadOnlyList<string> warnings);
}
