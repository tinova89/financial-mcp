using System.ComponentModel;
using FinancialMcp.Application.Statements.ImportStatement;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class StatementTools(IMediator mediator)
{
    [McpServerTool(Name = "import_statement"), Description(
        """
        Imports a CSV statement (checking account or credit card) into the database.

        ## Expected CSV format
        - Separator: `;`
        - Date format: `dd/MM/yyyy`
        - Decimal separator: dot (`.`)
        - Columns read: `Valor`, `Tipo`, `Status`, `Categoria`, `Descrição`/`Descricao`,
          `Data prevista`, `Data efetiva`, and — depending on whether `accountId` refers to
          a checking account or a credit card — either `Data Conciliado`, or `Venc. Fatura` /
          `Repetição`/`Repeticao` / `Parcela Atual` / `Parcela Total`.

        ## Parameters
        - **csvContent** — The full CSV content to import. Required.
        - **accountId** — `Guid` of the destination account for every row in this file: a
          checking account, or a credit card's own id (a credit card is an Account row via
          EF Core TPH). Required — a statement always belongs to exactly one account.

        ## Not a parameter
        - There's no `source`/statement-type flag — whether each row is parsed as a
          checking-account or credit-card statement is determined automatically by looking
          up `accountId`'s actual `Account.Kind`, not a caller-supplied label.

        ## Behavior
        - Invalid lines generate a warning and are skipped — a single malformed row never
          aborts the whole import; check `warnings` in the result for anything skipped.
        - Imported transactions are added but the whole batch commits in a single
          transaction (all-or-nothing at the database level, independent of per-line
          parsing warnings).

        ## Example
        ```json
        { "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "csvContent": "Valor;Tipo;Status;..." }
        ```

        ## Returns
        `ImportStatementResultDto` (linesProcessed, linesImported, warnings).

        ## Exceptions
        - `OperationCanceledException` — if the operation is canceled via `cancellationToken`.
        """)]
    public Task<ImportStatementResultDto> ImportStatementAsync(
        string csvContent, Guid accountId,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ImportStatementCommand(accountId, csvContent), cancellationToken);
}
