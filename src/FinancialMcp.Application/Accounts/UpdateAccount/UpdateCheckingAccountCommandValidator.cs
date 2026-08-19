using FluentValidation;

namespace FinancialMcp.Application.Accounts.UpdateAccount;

public sealed class UpdateCheckingAccountCommandValidator : AbstractValidator<UpdateCheckingAccountCommand>
{
    public UpdateCheckingAccountCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();

        When(x => x.DisplayName is not null, () =>
            RuleFor(x => x.DisplayName!).NotEmpty().MaximumLength(200));

        When(x => x.BankCode is not null, () =>
            RuleFor(x => x.BankCode!).NotEmpty().MaximumLength(200)
                .Must(code => FinancialBank.All.Any(b => b.BankCode == code))
                .WithMessage(x => $"BankCode \"{x.BankCode}\" não é reconhecido. Bancos suportados: " +
                    $"{string.Join(", ", FinancialBank.All.Select(b => b.BankCode))}."));

        When(x => x.BaseCurrencyCode is not null, () =>
            RuleFor(x => x.BaseCurrencyCode!).NotEmpty()
                .Must(code => FinancialCurrency.All.Any(c => c.CurrencyCode == code))
                .WithMessage(x => $"BaseCurrencyCode \"{x.BaseCurrencyCode}\" não é reconhecido. Moedas suportadas: " +
                    $"{string.Join(", ", FinancialCurrency.All.Select(c => c.CurrencyCode))}."));
    }
}
