using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentValidation;

namespace FinancialMcp.Application.Statements.ImportStatement;

public sealed class ImportStatementCommandValidator : AbstractValidator<ImportStatementCommand>
{
    public ImportStatementCommandValidator()
    {
        RuleFor(x => x.CsvContent).NotEmpty();

        When(x => x.Source == TransactionSource.CheckingAccount, () => RuleFor(x => x.AccountId).NotNull());
        When(x => x.Source == TransactionSource.CreditCard, () => RuleFor(x => x.CardId).NotNull());
    }
}
