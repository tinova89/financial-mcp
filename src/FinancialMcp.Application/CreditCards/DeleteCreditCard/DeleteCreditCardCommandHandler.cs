using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.CreditCards.DeleteCreditCard;

/// <summary>
/// Single handler for DeleteCreditCardCommand. The confirmation has already been
/// validated by ValidationBehavior before this handler is reached; here it only applies the soft delete.
/// </summary>
public sealed class DeleteCreditCardCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteCreditCardCommand>
{
    public async Task Handle(DeleteCreditCardCommand request, CancellationToken cancellationToken)
    {
        var creditCard = await db.CreditCards
            .FirstOrDefaultAsync(c => c.Id == request.CreditCardId, cancellationToken);

        if (creditCard is null)
        {
            throw new NotFoundException(nameof(CreditCard), request.CreditCardId);
        }

        creditCard.MarkAsDeleted();

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).
    }
}
