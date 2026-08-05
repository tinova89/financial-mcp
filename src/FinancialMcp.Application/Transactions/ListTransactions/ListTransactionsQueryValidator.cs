using FluentValidation;

namespace FinancialMcp.Application.Transactions.ListTransactions;

public sealed class ListTransactionsQueryValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x.PeriodoFim)
            .GreaterThanOrEqualTo(x => x.PeriodoInicio!.Value)
            .When(x => x.PeriodoInicio is not null && x.PeriodoFim is not null)
            .WithMessage("PeriodoFim deve ser maior ou igual a PeriodoInicio.");

        RuleFor(x => x.Mes).InclusiveBetween(1, 12).When(x => x.Mes is not null);
    }
}
