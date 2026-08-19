using FluentValidation;

namespace FinancialMcp.Application.Accounts.DeleteAccount;

public sealed class DeleteCheckingAccountCommandValidator : AbstractValidator<DeleteCheckingAccountCommand>
{
    public DeleteCheckingAccountCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();

        RuleFor(x => x.Confirm)
            .Equal(true)
            .WithMessage("Operação destrutiva: é necessário confirmar explicitamente (Confirm = true) antes de excluir a conta.");
    }
}
