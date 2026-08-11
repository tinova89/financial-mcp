using System.ComponentModel;
using FinancialMcp.Application.Statements.ImportStatement;
using FinancialMcp.Domain.Enums;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class StatementTools(IMediator mediator)
{
    [McpServerTool(Name = "import_statement"), Description(
        @"
        # `ImportStatementAsync` (MCP tool: `import_statement`)

        Server-side tool that imports a CSV statement (checking account or credit card) into the database by dispatching an `ImportStatementCommand` through `IMediator`.

        ## Expected CSV format
        - Separator: `;`
        - Date format: `dd/MM/yyyy`
        - Decimal separator: dot (`.`)

        ## Parameters
        - `source` (string)  
          Name of the transaction source mapped to the `TransactionSource` enum. The value is parsed with `Enum.Parse<TransactionSource>(source)` and must match one of the enum member names exactly (case-sensitive). Allowed values:
          - `CheckingAccount`
          - `CreditCard`  

          Passing an invalid value will throw an `ArgumentException`.

        - `csvContent` (string)  
          The full CSV content to import.

        - `accountId` (`Guid?`)
          Required in practice: associates imported transactions with their destination —
          the checking account itself for `CheckingAccount` rows, or the credit card's own id
          for `CreditCard` rows (a credit card is an Account row via EF Core TPH).

        - `cancellationToken` (`CancellationToken`)
          Token to cancel the import operation.

        ## Returns
        `Task<ImportStatementResultDto>` — result summary (created/updated records, errors, etc.).

        ## Exceptions
        - `ArgumentException` — if `source` cannot be parsed to `TransactionSource`.
        - `OperationCanceledException` — if the operation is canceled via `cancellationToken`."
    )]
    public Task<ImportStatementResultDto> ImportStatementAsync(
        string source, string csvContent, Guid? accountId = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ImportStatementCommand(Enum.Parse<TransactionSource>(source), accountId, csvContent), cancellationToken);
}
