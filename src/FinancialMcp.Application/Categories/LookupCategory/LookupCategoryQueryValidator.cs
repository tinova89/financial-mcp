using FluentValidation;

namespace FinancialMcp.Application.Categories.LookupCategory;

public sealed class LookupCategoryQueryValidator : AbstractValidator<LookupCategoryQuery>
{
    public LookupCategoryQueryValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
    }
}
