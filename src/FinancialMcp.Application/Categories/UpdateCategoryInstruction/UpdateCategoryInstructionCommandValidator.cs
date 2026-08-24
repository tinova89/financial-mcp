using FluentValidation;

namespace FinancialMcp.Application.Categories.UpdateCategoryInstruction;

public sealed class UpdateCategoryInstructionCommandValidator : AbstractValidator<UpdateCategoryInstructionCommand>
{
    public UpdateCategoryInstructionCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Instruction).NotEmpty().MaximumLength(2000);
    }
}
