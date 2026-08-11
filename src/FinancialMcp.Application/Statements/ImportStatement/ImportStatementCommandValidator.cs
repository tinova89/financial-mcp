using FluentValidation;

namespace FinancialMcp.Application.Statements.ImportStatement;

public sealed class ImportStatementCommandValidator : AbstractValidator<ImportStatementCommand>
{
    public ImportStatementCommandValidator()
    {
        RuleFor(x => x.CsvContent).NotEmpty();

        // AccountId is required regardless of source: the checking account for CheckingAccount
        // rows, or the CreditCard's own id for CreditCard rows (it's an Account row via TPH).
        RuleFor(x => x.AccountId).NotNull();
    }
}
