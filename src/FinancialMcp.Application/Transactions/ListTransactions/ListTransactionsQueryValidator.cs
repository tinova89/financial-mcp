using FluentValidation;

namespace FinancialMcp.Application.Transactions.ListTransactions;

public sealed class ListTransactionsQueryValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x.Type).IsInEnum().When(x => x.Type is not null);

        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);

        RuleFor(x => x.PeriodEnd)
            .GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("PeriodoFim deve ser maior ou igual a PeriodoInicio.");

        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month is not null && x.Month > 0);
    }
}
