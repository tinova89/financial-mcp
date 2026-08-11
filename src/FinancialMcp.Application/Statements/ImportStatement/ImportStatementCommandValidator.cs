using FluentValidation;

namespace FinancialMcp.Application.Statements.ImportStatement;

public sealed class ImportStatementCommandValidator : AbstractValidator<ImportStatementCommand>
{
    public ImportStatementCommandValidator()
    {
        RuleFor(x => x.CsvContent).NotEmpty();

        // Required regardless of the referenced account's kind: the checking account for
        // checking-account rows, or the CreditCard's own id for credit-card rows (it's an
        // Account row via TPH).
        RuleFor(x => x.AccountId).NotEmpty();
    }
}
