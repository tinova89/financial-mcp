using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Categories.LookupCategory;

public sealed class LookupCategoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<LookupCategoryQuery, CategoryLookupResultDto?>
{
    public async Task<CategoryLookupResultDto?> Handle(LookupCategoryQuery request, CancellationToken cancellationToken)
    {
        var normalizedDescription = request.Description.Trim();

        var mapping = await db.DescriptionCategoryMappings
            .AsNoTracking()
            .Include(m => m.Category).ThenInclude(c => c.ParentCategory)
            .FirstOrDefaultAsync(m => m.Description.ToLower() == normalizedDescription.ToLower(), cancellationToken);

        return mapping is null
            ? null
            : new CategoryLookupResultDto(mapping.CategoryId, mapping.Category.ParentCategoryName, mapping.Category.Subcategory);
    }
}
