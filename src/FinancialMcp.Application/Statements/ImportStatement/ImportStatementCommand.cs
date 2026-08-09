using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Imports a new CSV statement (checking account or credit card) into the database. Corresponds to the MCP tool
/// `import_statement`. Source format: ";" separator, "dd/mm/yyyy" dates,
/// dot-decimal (see CLAUDE.md > MCP and Code Conventions).
/// </summary>
public sealed record ImportStatementCommand(
    string Source,          // "ContaCorrente" | "CartaoCredito"
    Guid? AccountId,
    Guid? CardId,
    string CsvContent) : IRequest<ImportStatementResultDto>, ITransactionalRequest;

public sealed record ImportStatementResultDto(
    int LinesProcessed,
    int LinesImported,
    IReadOnlyList<string> Warnings);
