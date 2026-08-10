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
            .Include(a => a.CreditCards)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !(a is CreditCard), cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        return new AccountDto(
            account.Id, account.DisplayName, account.BankCode, account.InitialAmount,
            account.Kind.ToString(), account.BaseCurrencyCode, account.CreditCards.Select(c => c.Id).ToList());
    }
}
