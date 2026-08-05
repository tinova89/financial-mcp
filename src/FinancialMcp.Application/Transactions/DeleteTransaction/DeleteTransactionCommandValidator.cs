using FluentValidation;

namespace FinancialMcp.Application.Transactions.DeleteTransaction;

public sealed class DeleteTransactionCommandValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();

        RuleFor(x => x.Confirm)
            .Equal(true)
            .WithMessage("Operação destrutiva: é necessário confirmar explicitamente (Confirm = true) antes de excluir a transação.");
    }
}
