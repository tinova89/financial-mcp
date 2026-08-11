using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Domain.Enums;
using MediatR;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Imports a new CSV statement (checking account or credit card) into the database. Corresponds to the MCP tool
/// `import_statement`. Source format: ";" separator, "dd/mm/yyyy" dates,
/// dot-decimal (see CLAUDE.md > MCP and Code Conventions).
/// </summary>
public sealed record ImportStatementCommand(
    TransactionSource Source,
    Guid? AccountId,
    string CsvContent) : IRequest<ImportStatementResultDto>, ITransactionalRequest;

/// <summary>
/// Represents the result of importing statements, including the number of lines processed, the number successfully
/// imported, and any warnings.
/// </summary>
/// <remarks>Counts are non-negative; LinesImported is less than or equal to LinesProcessed.</remarks>
/// <param name="LinesProcessed">Total number of lines examined during the import operation.</param>
/// <param name="LinesImported">Number of lines successfully imported.</param>
/// <param name="Warnings">Read-only list of warnings produced during the import.</param>
public sealed record ImportStatementResultDto(
    int LinesProcessed,
    int LinesImported,
    IReadOnlyList<string> Warnings);
