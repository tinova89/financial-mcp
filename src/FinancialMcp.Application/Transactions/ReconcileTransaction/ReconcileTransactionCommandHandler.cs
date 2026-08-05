using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.ReconcileTransaction;

public sealed class ReconcileTransactionCommandHandler(IApplicationDbContext db, IPublisher publisher)
    : IRequestHandler<ReconcileTransactionCommand>
{
    public async Task Handle(ReconcileTransactionCommand request, CancellationToken cancellationToken)
    {
        var t = await db.Transacoes
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (t is null)
        {
            throw new NotFoundException(nameof(Transacao), request.TransactionId);
        }

        t.Status = StatusTransacao.Conciliado;

        if (t.Origem == OrigemTransacao.ContaCorrente)
        {
            t.DataConciliado = request.DataConciliado ?? DateOnly.FromDateTime(DateTime.UtcNow);
        }

        // Publicado após a alteração em memória; o commit real ocorre no TransactionBehavior.
        await publisher.Publish(new TransactionReconciledNotification(t.Id), cancellationToken);
    }
}
