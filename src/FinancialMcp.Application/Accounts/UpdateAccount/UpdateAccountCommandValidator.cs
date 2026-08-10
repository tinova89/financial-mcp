using FluentValidation;

namespace FinancialMcp.Application.Accounts.UpdateAccount;

public sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();

        When(x => x.Name is not null, () => RuleFor(x => x.Name!).NotEmpty().MaximumLength(200));
        When(x => x.Bank is not null, () => RuleFor(x => x.Bank!).NotEmpty().MaximumLength(200));
    }
}
