using FluentValidation;

namespace FinancialMcp.Application.Accounts.CreateAccount;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Bank).NotEmpty().MaximumLength(200);
    }
}
