using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.CreditCards.CreateCreditCard;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.CreditCards.GetCreditCard;

public sealed class GetCreditCardQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCreditCardQuery, CreditCardDto>
{
    public async Task<CreditCardDto> Handle(GetCreditCardQuery request, CancellationToken cancellationToken)
    {
        var creditCard = await db.CreditCards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CreditCardId, cancellationToken);

        if (creditCard is null)
        {
            throw new NotFoundException(nameof(CreditCard), request.CreditCardId);
        }

        return new CreditCardDto(
            creditCard.Id, creditCard.DisplayName, creditCard.BankCode, creditCard.InitialAmount,
            creditCard.Kind.ToString(), creditCard.BaseCurrencyCode, creditCard.ClosingDay, creditCard.DueDay,
            creditCard.PaymentAccountId);
    }
}
