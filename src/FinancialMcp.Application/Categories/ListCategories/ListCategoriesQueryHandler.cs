using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Categories.ListCategories;

public sealed class ListCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var rawCategories = await db.Transactions.AsNoTracking()
            .Select(t => t.RawCategory)
            .Distinct()
            .ToListAsync(cancellationToken);

        return rawCategories
            .Select(FinancialMcp.Domain.ValueObjects.Category.Parse)
            .GroupBy(c => c.ParentCategory)
            .Select(g => new CategoryDto(
                g.Key,
                g.Where(c => c.Subcategory is not null)
                 .Select(c => c.Subcategory!)
                 .Distinct()
                 .OrderBy(s => s)
                 .ToList()))
            .OrderBy(c => c.ParentCategory)
            .ToList();
    }
}
