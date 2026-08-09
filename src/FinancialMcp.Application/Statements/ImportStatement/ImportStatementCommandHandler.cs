using FinancialMcp.Application.Common.Interfaces;
using MediatR;

namespace FinancialMcp.Application.Statements.ImportStatement;

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
