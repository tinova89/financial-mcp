using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.DeleteTransaction;

/// <summary>
/// Handler único para DeleteTransactionCommand. A confirmação já foi validada pelo
/// ValidationBehavior antes deste handler ser alcançado; aqui apenas aplica o soft delete.
/// </summary>
public sealed class DeleteTransactionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transacao = await db.Transacoes
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transacao is null)
        {
            throw new NotFoundException(nameof(Transacao), request.TransactionId);
        }

        transacao.MarkAsDeleted();

        // SaveChangesAsync final é feito pelo TransactionBehavior (commit da transação de banco).
    }
}
