using FluentValidation;

namespace FinancialMcp.Application.CreditCards.DeleteCreditCard;

public sealed class DeleteCreditCardCommandValidator : AbstractValidator<DeleteCreditCardCommand>
{
    public DeleteCreditCardCommandValidator()
    {
        RuleFor(x => x.CreditCardId).NotEmpty();

        RuleFor(x => x.Confirm)
            .Equal(true)
            .WithMessage("Operação destrutiva: é necessário confirmar explicitamente (Confirm = true) antes de excluir o cartão de crédito.");
    }
}
