using FinancialMcp.Application.Categories.LookupCategory;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Categories.UpdateCategoryInstruction;

public sealed class UpdateCategoryInstructionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateCategoryInstructionCommand, CategoryInstructionDto>
{
    public async Task<CategoryInstructionDto> Handle(UpdateCategoryInstructionCommand request, CancellationToken cancellationToken)
    {
        var category = await db.TransactionCategories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(nameof(TransactionCategory), request.CategoryId);
        }

        category.Instruction = request.Instruction;

        return new CategoryInstructionDto(category.Id, category.ParentCategoryName, category.Subcategory, category.Instruction);
    }
}
