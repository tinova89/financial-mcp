using FinancialApp.Model;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.CreditCards.CreateCreditCard;

/// <summary>
/// Single handler for CreateCreditCardCommand. Kind is always hard-coded to Credit,
/// never taken from the request. Validates that PaymentAccountId resolves to an
/// existing, non-CreditCard account before persisting.
/// </summary>
public sealed class CreateCreditCardCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCreditCardCommand, CreditCardDto>
{
    public async Task<CreditCardDto> Handle(CreateCreditCardCommand request, CancellationToken cancellationToken)
    {
        var paymentAccountExists = await db.Accounts
            .AnyAsync(a => a.Id == request.PaymentAccountId && !(a is CreditCard), cancellationToken);

        if (!paymentAccountExists)
        {
            throw new NotFoundException(nameof(Account), request.PaymentAccountId);
        }

        var creditCard = new CreditCard
        {
            DisplayName = request.DisplayName,
            BankCode = request.BankCode,
            BaseCurrencyCode = request.BaseCurrencyCode,
            InitialAmount = request.InitialAmount,
            Kind = FinancialAccountKind.Credit,
            ClosingDay = request.ClosingDay,
            DueDay = request.DueDay,
            PaymentAccountId = request.PaymentAccountId,
        };

        db.CreditCards.Add(creditCard);

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new CreditCardDto(
            creditCard.Id, creditCard.DisplayName, creditCard.BankCode, creditCard.InitialAmount,
            creditCard.Kind.ToString(), creditCard.BaseCurrencyCode, creditCard.ClosingDay, creditCard.DueDay,
            creditCard.PaymentAccountId);
    }
}
