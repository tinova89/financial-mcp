using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    public async Task<ImportStatementResultDto> Handle(ImportStatementCommand request, CancellationToken cancellationToken)
    {
        // Whether this is a checking-account or credit-card statement is determined by the
        // referenced account's actual type, not a separately-supplied flag.
        var isCreditCard = await db.Accounts
            .AnyAsync(a => a.Id == request.AccountId && a is CreditCard, cancellationToken);

        var transactions = parser.Parse(request.CsvContent, isCreditCard, request.AccountId, out var warnings);

        foreach (var transaction in transactions)
        {
            db.Transactions.Add(transaction);
        }

        // Final SaveChangesAsync is done by TransactionBehavior (single commit for the whole batch).

        return new ImportStatementResultDto(
            LinesProcessed: transactions.Count + warnings.Count,
            LinesImported: transactions.Count,
            Warnings: warnings);
    }
}
