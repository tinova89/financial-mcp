using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Accounts.GetAccount;

public sealed class GetCheckingAccountQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCheckingAccountQuery, CheckingAccountDto>
{
    public async Task<CheckingAccountDto> Handle(GetCheckingAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AsNoTracking()
            .Include(a => a.CreditCards)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !(a is CreditCard), cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        return new CheckingAccountDto(
            account.Id, account.DisplayName, account.BankCode, account.InitialAmount,
            account.Kind.ToString(), account.BaseCurrencyCode, account.CreditCards.Select(c => c.Id).ToList());
    }
}
