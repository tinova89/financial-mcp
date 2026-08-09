using FluentValidation;

namespace FinancialMcp.Application.Statements.ImportStatement;

public sealed class ImportStatementCommandValidator : AbstractValidator<ImportStatementCommand>
{
    private static readonly string[] ValidSources = ["CheckingAccount", "CreditCard"];

    public ImportStatementCommandValidator()
    {
        RuleFor(x => x.Source).Must(o => ValidSources.Contains(o));
        RuleFor(x => x.CsvContent).NotEmpty();

        When(x => x.Source == "CheckingAccount", () => RuleFor(x => x.AccountId).NotNull());
        When(x => x.Source == "CreditCard", () => RuleFor(x => x.CardId).NotNull());
    }
}
