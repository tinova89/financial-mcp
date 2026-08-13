using FinancialApp.Model;
using FluentValidation;

namespace FinancialMcp.Application.CreditCards.CreateCreditCard;

public sealed class CreateCreditCardCommandValidator : AbstractValidator<CreateCreditCardCommand>
{
    public CreateCreditCardCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);

        RuleFor(x => x.BankCode).NotEmpty().MaximumLength(200)
            .Must(code => FinancialBank.All.Any(b => b.BankCode == code))
            .WithMessage(x => $"BankCode \"{x.BankCode}\" não é reconhecido. Bancos suportados: " +
                $"{string.Join(", ", FinancialBank.All.Select(b => b.BankCode))}.");

        RuleFor(x => x.BaseCurrencyCode).NotEmpty()
            .Must(code => FinancialCurrency.All.Any(c => c.CurrencyCode == code))
            .WithMessage(x => $"BaseCurrencyCode \"{x.BaseCurrencyCode}\" não é reconhecido. Moedas suportadas: " +
                $"{string.Join(", ", FinancialCurrency.All.Select(c => c.CurrencyCode))}.");

        // Capped at 31, maximum for month; when the month is less than 31, consider the last day of the month — no "roll to next month" edge case to model.
        RuleFor(x => x.ClosingDay).InclusiveBetween((byte)1, (byte)31);
        RuleFor(x => x.DueDay).InclusiveBetween((byte)1, (byte)31);

        RuleFor(x => x.PaymentAccountId).NotEmpty();
    }
}
