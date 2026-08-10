using FinancialMcp.Domain.Enums;
using FluentValidation;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Validates fields according to the statement's source format (";" separator,
/// dd/mm/yyyy dates already parsed into DateOnly, dot-decimal already parsed into
/// decimal) before persisting — see CLAUDE.md > MCP.
/// </summary>
public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);

        RuleFor(x => x.Amount).NotEqual(0m);

        RuleFor(x => x.RawCategory).NotEmpty();

        RuleFor(x => x.ExpectedDate).NotEqual(default(DateOnly));

        // Credit card specific rules.
        When(x => x.Source == TransactionSource.CreditCard, () =>
        {
            RuleFor(x => x.CardId).NotNull()
                .WithMessage("CartaoId é obrigatório para transações de Cartão de Crédito.");

            RuleFor(x => x.InvoiceDueDate).NotNull()
                .WithMessage("VencimentoFatura é obrigatório para transações de Cartão de Crédito.");

            When(x => x.Recurrence == RecurrenceType.Installment, () =>
            {
                RuleFor(x => x.CurrentInstallment).NotNull().GreaterThan(0);
                RuleFor(x => x.TotalInstallments).NotNull()
                    .Must((cmd, total) => total is null || cmd.CurrentInstallment is null || total >= cmd.CurrentInstallment)
                    .WithMessage("ParcelaTotal deve ser maior ou igual a ParcelaAtual.");
            });
        });

        // Checking account specific rules.
        When(x => x.Source == TransactionSource.CheckingAccount, () =>
        {
            RuleFor(x => x.AccountId).NotNull()
                .WithMessage("ContaId é obrigatório para transações de Conta Corrente.");
        });
    }
}
