using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Accounts.ListAccounts;

public sealed class ListAccountsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListAccountsQuery, IReadOnlyList<AccountDto>>
{
    public async Task<IReadOnlyList<AccountDto>> Handle(ListAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await db.Accounts.AsNoTracking()
            .Include(a => a.Cards)
            .OrderBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);

        return accounts
            .Select(a => new AccountDto(
                a.Id, a.DisplayName, a.BankCode, a.InitialAmount,
                a.Kind.ToString(), a.BaseCurrencyCode, a.Cards.Select(c => c.Id).ToList()))
            .ToList();
    }
}
