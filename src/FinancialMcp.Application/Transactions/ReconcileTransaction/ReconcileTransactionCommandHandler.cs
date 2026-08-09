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
        var t = await db.Transactions
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (t is null)
        {
            throw new NotFoundException(nameof(Transacao), request.TransactionId);
        }

        t.Status = StatusTransacao.Conciliado;

        if (t.Origem == OrigemTransacao.ContaCorrente)
        {
            t.DataConciliado = request.ReconciledDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        }

        // Published after the in-memory change; the actual commit happens in TransactionBehavior.
        await publisher.Publish(new TransactionReconciledNotification(t.Id), cancellationToken);
    }
}
