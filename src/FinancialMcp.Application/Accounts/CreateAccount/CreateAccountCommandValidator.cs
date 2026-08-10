using FluentValidation;

namespace FinancialMcp.Application.Accounts.CreateAccount;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankCode).NotEmpty().MaximumLength(200);
    }
}
