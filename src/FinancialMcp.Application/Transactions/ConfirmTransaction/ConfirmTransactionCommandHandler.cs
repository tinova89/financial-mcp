using FinancialApp.Model;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.ConfirmTransaction;

public sealed class ConfirmTransactionCommandHandler(IApplicationDbContext db, IPublisher publisher)
    : IRequestHandler<ConfirmTransactionCommand>
{
    public async Task Handle(ConfirmTransactionCommand request, CancellationToken cancellationToken)
    {
        var t = await db.Transactions
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (t is null)
        {
            throw new NotFoundException(nameof(Transaction), request.TransactionId);
        }

        // Confirming = the transaction actually happened → Confirmed (Card #14), stamping ConfirmedAt.
        t.TransitionTo(TransactionStatus.Confirmed, DateTimeOffset.UtcNow);

        if (t.Account.Kind != FinancialAccountKind.Credit)
        {
            t.ConfirmationDate = request.ConfirmationDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        }

        // Published after the in-memory change; the actual commit happens in TransactionBehavior.
        await publisher.Publish(new TransactionConfirmedNotification(t.Id), cancellationToken);
    }
}
