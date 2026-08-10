using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.CreditCards.CreateCreditCard;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.CreditCards.UpdateCreditCard;

public sealed class UpdateCreditCardCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateCreditCardCommand, CreditCardDto>
{
    public async Task<CreditCardDto> Handle(UpdateCreditCardCommand request, CancellationToken cancellationToken)
    {
        var creditCard = await db.CreditCards
            .FirstOrDefaultAsync(c => c.Id == request.CreditCardId, cancellationToken);

        if (creditCard is null)
        {
            throw new NotFoundException(nameof(CreditCard), request.CreditCardId);
        }

        if (request.PaymentAccountId is not null)
        {
            var paymentAccountExists = await db.Accounts
                .AnyAsync(a => a.Id == request.PaymentAccountId && !(a is CreditCard), cancellationToken);

            if (!paymentAccountExists)
            {
                throw new NotFoundException(nameof(Account), request.PaymentAccountId.Value);
            }

            creditCard.PaymentAccountId = request.PaymentAccountId.Value;
        }

        if (request.DisplayName is not null) creditCard.DisplayName = request.DisplayName;
        if (request.BankCode is not null) creditCard.BankCode = request.BankCode;
        if (request.InitialAmount is not null) creditCard.InitialAmount = request.InitialAmount.Value;
        if (request.BaseCurrencyCode is not null) creditCard.BaseCurrencyCode = request.BaseCurrencyCode;
        if (request.ClosingDay is not null) creditCard.ClosingDay = request.ClosingDay.Value;
        if (request.DueDay is not null) creditCard.DueDay = request.DueDay.Value;

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new CreditCardDto(
            creditCard.Id, creditCard.DisplayName, creditCard.BankCode, creditCard.InitialAmount,
            creditCard.Kind.ToString(), creditCard.BaseCurrencyCode, creditCard.ClosingDay, creditCard.DueDay,
            creditCard.PaymentAccountId);
    }
}
