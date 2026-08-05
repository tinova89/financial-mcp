using FluentValidation;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    private static readonly string[] StatusValidos = ["Conciliado", "Agendado", "Nconciliado"];

    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();

        RuleFor(x => x.Status).Must(s => s is null || StatusValidos.Contains(s))
            .WithMessage($"Status deve ser um de: {string.Join(", ", StatusValidos)}.");

        RuleFor(x => x.Valor).NotEqual(0m).When(x => x.Valor is not null);
    }
}
