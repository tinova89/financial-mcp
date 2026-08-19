using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Accounts.ListAccounts;

public sealed class ListCheckingAccountsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListCheckingAccountsQuery, IReadOnlyList<CheckingAccountDto>>
{
    public async Task<IReadOnlyList<CheckingAccountDto>> Handle(ListCheckingAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => !(a is CreditCard))
            .Include(a => a.CreditCards)
            .OrderBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);

        return accounts
            .Select(a => new CheckingAccountDto(
                a.Id, a.DisplayName, a.BankCode, a.InitialAmount,
                a.Kind.ToString(), a.BaseCurrencyCode, a.CreditCards.Select(c => c.Id).ToList()))
            .ToList();
    }
}
