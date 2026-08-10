using FinancialMcp.Application.Common.Interfaces;
using MediatR;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Handles ImportStatementCommand requests by parsing CSV content into transactions and adding them to the application
/// database.
/// </summary>
/// <remarks>Parsed transactions are added to the DbContext but not saved here; TransactionBehavior performs the
/// final SaveChangesAsync. The handler produces an ImportStatementResultDto with lines processed, lines imported, and
/// any warnings.</remarks>
/// <param name="db">Application database context used to add parsed transactions.</param>
/// <param name="parser">Parser that converts CSV content and source information into transaction entities and collects parsing warnings.</param>
public sealed class ImportStatementCommandHandler(IApplicationDbContext db, IStatementCsvParser parser)
    : IRequestHandler<ImportStatementCommand, ImportStatementResultDto>
{
    public Task<ImportStatementResultDto> Handle(ImportStatementCommand request, CancellationToken cancellationToken)
    {
        var transactions = parser.Parse(request.CsvContent, request.Source, request.AccountId, request.CardId, out var warnings);

        foreach (var transaction in transactions)
        {
            db.Transactions.Add(transaction);
        }

        // Final SaveChangesAsync is done by TransactionBehavior (single commit for the whole batch).

        return Task.FromResult(new ImportStatementResultDto(
            LinesProcessed: transactions.Count + warnings.Count,
            LinesImported: transactions.Count,
            Warnings: warnings));
    }
}
