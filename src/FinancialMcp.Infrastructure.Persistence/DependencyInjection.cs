using FinancialMcp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialMcp.Infrastructure.Persistence;

/// <summary>
/// Registration of EF Core/Postgres persistence services that don't depend on the
/// Aspire host. FinancialMcp.Api's Program.cs is the single place that registers
/// ApplicationDbContext itself, via builder.AddNpgsqlDbContext (connection string,
/// resiliency, and telemetry come from Aspire service discovery) — registering it
/// again here via AddDbContext would create a conflicting second set of
/// DbContextOptions/IDbContextOptionsConfiguration descriptors for the same context.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services)
    {
        services.AddScoped<IApplicationDbContext>(provider =>
        {
            return provider.GetRequiredService<ApplicationDbContext>();
        });

        // TransactionBehavior (Application layer) depends on the base DbContext type to
        // stay decoupled from Infrastructure — map it to the concrete ApplicationDbContext here.
        services.AddScoped<DbContext>(provider =>
        {
            return provider.GetRequiredService<ApplicationDbContext>();
        });
        return services;
    }
}
