using FluentValidation;

namespace FinancialMcp.Application.Statements.ImportStatement;

public sealed class ImportStatementCommandValidator : AbstractValidator<ImportStatementCommand>
{
    private static readonly string[] OrigensValidas = ["ContaCorrente", "CartaoCredito"];

    public ImportStatementCommandValidator()
    {
        RuleFor(x => x.Origem).Must(o => OrigensValidas.Contains(o));
        RuleFor(x => x.CsvContent).NotEmpty();

        When(x => x.Origem == "ContaCorrente", () => RuleFor(x => x.ContaId).NotNull());
        When(x => x.Origem == "CartaoCredito", () => RuleFor(x => x.CartaoId).NotNull());
    }
}
