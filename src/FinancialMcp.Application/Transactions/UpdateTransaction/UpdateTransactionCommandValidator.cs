using FluentValidation;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();

        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);

        RuleFor(x => x.Amount).NotEqual(0m).When(x => x.Amount is not null);
    }
}
