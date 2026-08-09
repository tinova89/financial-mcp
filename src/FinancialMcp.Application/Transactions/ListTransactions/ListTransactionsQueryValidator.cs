using FluentValidation;

namespace FinancialMcp.Application.Transactions.ListTransactions;

public sealed class ListTransactionsQueryValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x.PeriodEnd)
            .GreaterThanOrEqualTo(x => x.PeriodStart!.Value)
            .When(x => x.PeriodStart is not null && x.PeriodEnd is not null)
            .WithMessage("PeriodoFim deve ser maior ou igual a PeriodoInicio.");

        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month is not null);
    }
}
