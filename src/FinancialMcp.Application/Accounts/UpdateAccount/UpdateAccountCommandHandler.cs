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
            .Include(a => a.CreditCards)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !(a is CreditCard), cancellationToken) ?? throw new NotFoundException(nameof(Account), request.AccountId);
        if (request.DisplayName is not null) account.DisplayName = request.DisplayName;
        if (request.BankCode is not null) account.BankCode = request.BankCode;
        if (request.InitialAmount is not null) account.InitialAmount = request.InitialAmount.Value;
        if (request.BaseCurrencyCode is not null) account.BaseCurrencyCode = request.BaseCurrencyCode;

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new AccountDto(
            account.Id, account.DisplayName, account.BankCode, account.InitialAmount,
            account.Kind.ToString(), account.BaseCurrencyCode, account.CreditCards.Select(c => c.Id).ToList());
    }
}
