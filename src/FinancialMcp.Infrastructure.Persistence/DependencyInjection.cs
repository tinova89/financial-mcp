using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialMcp.Infrastructure.Persistence;

/// <summary>
/// Registro de EF Core/Postgres, auth JWT customizado e serviços de infraestrutura.
/// Chamado a partir de FinancialMcp.Api (que também chama builder.AddNpgsqlDbContext
/// via integração Aspire para pegar a connection string por service discovery).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // A connection string real é injetada pelo Aspire via AddNpgsqlDbContext
        // no Program.cs da Api (ConnectionStrings:financialmcp-db). Aqui registramos
        // apenas o provider e as opções que não dependem do host Aspire.
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("financialmcp-db");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
        });
        return services;
    }
}
