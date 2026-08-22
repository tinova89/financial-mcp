using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Validates fields according to the statement's format (";" separator, dd/mm/yyyy dates
/// already parsed into DateOnly, dot-decimal already parsed into decimal) before
/// persisting — see CLAUDE.md > MCP.
/// </summary>
public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);

        RuleFor(x => x.Amount).NotEqual(0m);

        RuleFor(x => x.RawCategory).NotEmpty();

        RuleFor(x => x.ExpectedDate).NotEqual(default(DateOnly));

        RuleFor(x => x.ReconciledDate).NotNull()
            .When(x => x.Status == TransactionStatus.Reconciled)
            .WithMessage("DataConciliado é obrigatório quando Status = Reconciliado.");

        // Mandatory regardless of the referenced account's kind: the checking account for
        // checking-account transactions, or the CreditCard's own id for credit-card transactions.
        RuleFor(x => x.AccountId).NotEmpty()
            .WithMessage("AccountId é obrigatório (conta corrente ou cartão de crédito).");

        // Credit-card-specific rules — determined by looking up the referenced account's
        // actual type (Account.Kind), not a separately-supplied Source flag that could
        // disagree with what AccountId actually points at.
        WhenAsync(async (cmd, ct) => await db.Accounts.AnyAsync(a => a.Id == cmd.AccountId && a is CreditCard, ct), () =>
        {
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
    }
}
