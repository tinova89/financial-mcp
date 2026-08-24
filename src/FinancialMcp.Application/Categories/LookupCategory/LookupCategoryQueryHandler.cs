using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Categories.LookupCategory;

public sealed class LookupCategoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<LookupCategoryQuery, IReadOnlyList<CategoryInstructionDto>>
{
    public async Task<IReadOnlyList<CategoryInstructionDto>> Handle(LookupCategoryQuery request, CancellationToken cancellationToken)
    {
        var categories = await db.TransactionCategories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Where(c => c.Instruction != null)
            .ToListAsync(cancellationToken);

        return categories
            .Select(c => new CategoryInstructionDto(c.Id, c.ParentCategoryName, c.Subcategory, c.Instruction!))
            .OrderBy(c => c.ParentCategory)
            .ThenBy(c => c.Subcategory)
            .ToList();
    }
}
