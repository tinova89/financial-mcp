using FluentValidation;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

public sealed class GetBudgetStatusQueryValidator : AbstractValidator<GetBudgetStatusQuery>
{
    public GetBudgetStatusQueryValidator()
    {
        RuleFor(x => x.Ano).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Mes).InclusiveBetween(1, 12);
    }
}
