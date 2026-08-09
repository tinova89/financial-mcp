using System.ComponentModel;
using FinancialMcp.Application.Statements.ImportStatement;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class StatementTools(IMediator mediator)
{
    [McpServerTool(Name = "import_statement"), Description(
        "Imports a new CSV statement (checking account or credit card) into the database. " +
        "Expected format: ';' separator, 'dd/mm/yyyy' dates, dot-decimal.")]
    public Task<ImportStatementResultDto> ImportStatementAsync(
        string source, string csvContent, Guid? accountId = null, Guid? cardId = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ImportStatementCommand(source, accountId, cardId, csvContent), cancellationToken);
}
