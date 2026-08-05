using FluentValidation;

namespace FinancialMcp.Application.BalanceProjection.GetBalanceProjection;

public sealed class GetBalanceProjectionQueryValidator : AbstractValidator<GetBalanceProjectionQuery>
{
    public GetBalanceProjectionQueryValidator()
    {
        RuleFor(x => x.ContaId).NotEmpty();
        RuleFor(x => x.MesesAFrente).InclusiveBetween(1, 24);
    }
}
