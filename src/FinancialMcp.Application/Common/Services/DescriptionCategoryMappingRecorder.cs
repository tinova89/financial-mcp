using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinancialMcp.Application.Common.Services;

/// <summary>
/// See IDescriptionCategoryMappingRecorder. Runs entirely detached from the caller: it
/// opens its own DI scope (its own IApplicationDbContext instance) on a background task
/// instead of reusing the request's scoped DbContext, so it never competes with the
/// request's own SaveChangesAsync/commit and never delays the response.
/// </summary>
public sealed class DescriptionCategoryMappingRecorder(
    IServiceScopeFactory scopeFactory,
    ILogger<DescriptionCategoryMappingRecorder> logger) : IDescriptionCategoryMappingRecorder
{
    public void Record(string description, Guid categoryId)
    {
        var normalizedDescription = description.Trim();

        if (normalizedDescription.Length == 0)
        {
            return;
        }

        _ = Task.Run(() => RecordAsync(normalizedDescription, categoryId));
    }

    private async Task RecordAsync(string description, Guid categoryId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var mapping = await db.DescriptionCategoryMappings
                .FirstOrDefaultAsync(m => m.Description.ToLower() == description.ToLower());

            if (mapping is null)
            {
                db.DescriptionCategoryMappings.Add(new DescriptionCategoryMapping
                {
                    Description = description,
                    CategoryId = categoryId
                });
            }
            else if (mapping.CategoryId != categoryId)
            {
                mapping.CategoryId = categoryId;
            }
            else
            {
                return;
            }

            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Best-effort learning subroutine — never let it surface to (or block) the
            // create/update transaction flow that triggered it.
            logger.LogWarning(ex, "Falha ao registrar mapeamento descrição→categoria para '{Description}'", description);
        }
    }
}
