using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Categories.ListCategories;

public sealed class ListCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoriaDto>>
{
    public async Task<IReadOnlyList<CategoriaDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var brutas = await db.Transacoes.AsNoTracking()
            .Select(t => t.CategoriaBruta)
            .Distinct()
            .ToListAsync(cancellationToken);

        return brutas
            .Select(FinancialMcp.Domain.ValueObjects.Categoria.Parse)
            .GroupBy(c => c.CategoriaMae)
            .Select(g => new CategoriaDto(
                g.Key,
                g.Where(c => c.Subcategoria is not null)
                 .Select(c => c.Subcategoria!)
                 .Distinct()
                 .OrderBy(s => s)
                 .ToList()))
            .OrderBy(c => c.CategoriaMae)
            .ToList();
    }
}
