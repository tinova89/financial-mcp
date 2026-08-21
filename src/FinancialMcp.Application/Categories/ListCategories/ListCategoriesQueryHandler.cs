using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Categories.ListCategories;

public sealed class ListCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await db.TransactionCategories.AsNoTracking()
            .ToListAsync(cancellationToken);

        return categories
            .Where(c => c.ParentCategoryId is null)
            .Select(parent => new CategoryDto(
                parent.Id,
                parent.Name,
                categories
                    .Where(c => c.ParentCategoryId == parent.Id)
                    .Select(c => c.Name)
                    .OrderBy(s => s)
                    .ToList()))
            .OrderBy(c => c.ParentCategory)
            .ToList();
    }
}
