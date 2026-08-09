using FluentValidation;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

public sealed class GetBudgetStatusQueryValidator : AbstractValidator<GetBudgetStatusQuery>
{
    public GetBudgetStatusQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
