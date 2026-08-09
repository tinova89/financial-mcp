using FluentValidation;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Validates fields according to the statement's source format (";" separator,
/// dd/mm/yyyy dates already parsed into DateOnly, dot-decimal already parsed into
/// decimal) before persisting — see CLAUDE.md > MCP.
/// </summary>
public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    private static readonly string[] ValidSources = ["ContaCorrente", "CartaoCredito"];
    private static readonly string[] ValidTypes = ["Despesa", "Receita", "Transferencia", "Pagamento"];

    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Source).Must(o => ValidSources.Contains(o))
            .WithMessage($"Origem deve ser uma de: {string.Join(", ", ValidSources)}.");

        RuleFor(x => x.Type).Must(t => ValidTypes.Contains(t))
            .WithMessage($"Tipo deve ser um de: {string.Join(", ", ValidTypes)}.");

        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);

        RuleFor(x => x.Amount).NotEqual(0m);

        RuleFor(x => x.RawCategory).NotEmpty();

        RuleFor(x => x.ExpectedDate).NotEqual(default(DateOnly));

        // Credit card specific rules.
        When(x => x.Source == "CartaoCredito", () =>
        {
            RuleFor(x => x.CardId).NotNull()
                .WithMessage("CartaoId é obrigatório para transações de Cartão de Crédito.");

            RuleFor(x => x.InvoiceDueDate).NotNull()
                .WithMessage("VencimentoFatura é obrigatório para transações de Cartão de Crédito.");

            When(x => x.Recurrence == "Parcelado", () =>
            {
                RuleFor(x => x.CurrentInstallment).NotNull().GreaterThan(0);
                RuleFor(x => x.TotalInstallments).NotNull()
                    .Must((cmd, total) => total is null || cmd.CurrentInstallment is null || total >= cmd.CurrentInstallment)
                    .WithMessage("ParcelaTotal deve ser maior ou igual a ParcelaAtual.");
            });
        });

        // Checking account specific rules.
        When(x => x.Source == "ContaCorrente", () =>
        {
            RuleFor(x => x.AccountId).NotNull()
                .WithMessage("ContaId é obrigatório para transações de Conta Corrente.");
        });
    }
}
