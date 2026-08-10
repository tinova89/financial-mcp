using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Accounts.GetAccount;

public sealed class GetAccountQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAccountQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AsNoTracking()
            .Include(a => a.Cards)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        return new AccountDto(account.Id, account.Name, account.Bank, account.Cards.Select(c => c.Id).ToList());
    }
}
