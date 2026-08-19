using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;

namespace FinancialMcp.Application.Accounts.CreateAccount;

/// <summary>Single handler for CreateAccountCommand — orchestrates persistence of the new account.</summary>
public sealed class CreateCheckingAccountCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCheckingAccountCommand, CheckingAccountDto>
{
    public async Task<CheckingAccountDto> Handle(CreateCheckingAccountCommand request, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            DisplayName = request.DisplayName,
            BankCode = request.BankCode,
            BaseCurrencyCode = request.BaseCurrencyCode,
            InitialAmount = request.InitialAmount,
        };

        db.Accounts.Add(account);

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new CheckingAccountDto(
            account.Id, account.DisplayName, account.BankCode, account.InitialAmount,
            account.Kind.ToString(), account.BaseCurrencyCode, []);
    }
}
