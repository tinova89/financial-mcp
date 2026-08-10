using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.CreditCards.CreateCreditCard;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.CreditCards.ListCreditCards;

public sealed class ListCreditCardsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListCreditCardsQuery, IReadOnlyList<CreditCardDto>>
{
    public async Task<IReadOnlyList<CreditCardDto>> Handle(ListCreditCardsQuery request, CancellationToken cancellationToken)
    {
        var creditCards = await db.CreditCards.AsNoTracking()
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return creditCards
            .Select(c => new CreditCardDto(
                c.Id, c.DisplayName, c.BankCode, c.InitialAmount,
                c.Kind.ToString(), c.BaseCurrencyCode, c.ClosingDay, c.DueDay, c.PaymentAccountId))
            .ToList();
    }
}
