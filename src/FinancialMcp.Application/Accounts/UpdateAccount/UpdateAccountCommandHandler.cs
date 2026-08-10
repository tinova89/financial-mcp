using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Accounts.UpdateAccount;

public sealed class UpdateAccountCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts
            .Include(a => a.Cards)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        if (request.Name is not null) account.DisplayName = request.Name;
        if (request.Bank is not null) account.BankCode = request.Bank;

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new AccountDto(account.Id, account.DisplayName, account.BankCode, account.Cards.Select(c => c.Id).ToList());
    }
}
