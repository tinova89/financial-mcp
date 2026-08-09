using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.DeleteTransaction;

/// <summary>
/// Single handler for DeleteTransactionCommand. The confirmation has already been
/// validated by ValidationBehavior before this handler is reached; here it only applies the soft delete.
/// </summary>
public sealed class DeleteTransactionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException(nameof(Transacao), request.TransactionId);
        }

        transaction.MarkAsDeleted();

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).
    }
}
