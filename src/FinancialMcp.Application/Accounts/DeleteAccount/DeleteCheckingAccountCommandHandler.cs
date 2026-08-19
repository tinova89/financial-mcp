using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Accounts.DeleteAccount;

/// <summary>
/// Single handler for DeleteAccountCommand. The confirmation has already been
/// validated by ValidationBehavior before this handler is reached; here it only applies the soft delete.
/// </summary>
public sealed class DeleteCheckingAccountCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteCheckingAccountCommand>
{
    public async Task Handle(DeleteCheckingAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !(a is CreditCard), cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        account.MarkAsDeleted();

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).
    }
}
