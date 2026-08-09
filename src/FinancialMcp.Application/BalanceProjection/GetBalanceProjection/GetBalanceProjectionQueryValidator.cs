using FluentValidation;

namespace FinancialMcp.Application.BalanceProjection.GetBalanceProjection;

public sealed class GetBalanceProjectionQueryValidator : AbstractValidator<GetBalanceProjectionQuery>
{
    public GetBalanceProjectionQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.MonthsAhead).InclusiveBetween(1, 24);
    }
}
