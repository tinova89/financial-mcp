using FluentValidation;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    private static readonly string[] ValidStatuses = ["Reconciled", "Scheduled", "Unreconciled"];

    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();

        RuleFor(x => x.Status).Must(s => s is null || ValidStatuses.Contains(s))
            .WithMessage($"Status deve ser um de: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.Amount).NotEqual(0m).When(x => x.Amount is not null);
    }
}
