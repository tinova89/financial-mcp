using FinancialMcp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialMcp.Infrastructure.Persistence;

/// <summary>
/// Registration of EF Core/Postgres persistence services. Called from
/// FinancialMcp.Api (which also calls builder.AddNpgsqlDbContext via Aspire
/// integration to get the connection string via service discovery).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, string connectionStringName, IConfiguration configuration)
    {
        // The real connection string is injected by Aspire via AddNpgsqlDbContext
        // in the Api's Program.cs (ConnectionStrings:financialmcp-db). Here we register
        // only the provider and the options that don't depend on the Aspire host.
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<IApplicationDbContext>(provider =>
        {
            return provider.GetRequiredService<ApplicationDbContext>();
        });
        return services;
    }
}
