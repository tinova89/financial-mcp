using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.CreditCards.CreateCreditCard;

/// <summary>
/// Single handler for CreateCreditCardCommand. Kind is never taken from the request —
/// it's always Credit via CreditCard.Kind's override (see Account.Kind). Validates that
/// PaymentAccountId resolves to an existing, non-CreditCard account before persisting.
/// </summary>
public sealed class CreateCreditCardCommandHandler(IApplicationDbContext db, ICurrentGroupService currentGroup)
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

        // Kind is not set here — it's computed as always Credit via CreditCard.Kind's override.
        var creditCard = new CreditCard
        {
            DisplayName = request.DisplayName,
            BankCode = request.BankCode,
            BaseCurrencyCode = request.BaseCurrencyCode,
            InitialAmount = request.InitialAmount,
            ClosingDay = request.ClosingDay,
            DueDay = request.DueDay,
            PaymentAccountId = request.PaymentAccountId,
            // Enforced present by RequireGroupHeaderMiddleware before this handler ever runs.
            Group = currentGroup.Group ?? throw new InvalidOperationException(
                "Cabeçalho X-Account-Group ausente — deveria ter sido bloqueado por RequireGroupHeaderMiddleware."),
        };

        db.CreditCards.Add(creditCard);

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new CreditCardDto(
            creditCard.Id, creditCard.DisplayName, creditCard.BankCode, creditCard.InitialAmount,
            creditCard.Kind.ToString(), creditCard.BaseCurrencyCode, creditCard.ClosingDay, creditCard.DueDay,
            creditCard.PaymentAccountId);
    }
}
