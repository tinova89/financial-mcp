using FinancialMcp.Application.Common.Interfaces;
using MediatR;

namespace FinancialMcp.Application.Statements.ImportStatement;

public sealed class ImportStatementCommandHandler(IApplicationDbContext db, IStatementCsvParser parser)
    : IRequestHandler<ImportStatementCommand, ImportStatementResultDto>
{
    public Task<ImportStatementResultDto> Handle(ImportStatementCommand request, CancellationToken cancellationToken)
    {
        var transacoes = parser.Parse(request.CsvContent, request.Origem, request.ContaId, request.CartaoId, out var avisos);

        foreach (var transacao in transacoes)
        {
            db.Transacoes.Add(transacao);
        }

        // SaveChangesAsync final é feito pelo TransactionBehavior (commit único para todo o lote).

        return Task.FromResult(new ImportStatementResultDto(
            LinhasProcessadas: transacoes.Count + avisos.Count,
            LinhasImportadas: transacoes.Count,
            Avisos: avisos));
    }
}
