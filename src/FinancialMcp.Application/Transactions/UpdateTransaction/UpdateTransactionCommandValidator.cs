using FinancialMcp.Domain.Entities;
using FluentValidation;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();

        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);

        RuleFor(x => x.Amount).NotEqual(0m).When(x => x.Amount is not null);

        // Card #14: free-text fields on transactions are capped at 256 characters.
        RuleFor(x => x.RawCategory).MaximumLength(Transaction.FreeTextMaxLength).When(x => x.RawCategory is not null);
    }
}
