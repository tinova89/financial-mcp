using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Statements.ImportStatement;
using FinancialMcp.Infrastructure.Interfaces;
using FinancialMcp.Infrastructure.Persistence;
using FinancialMcp.Infrastructure.Statements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace FinancialMcp.Infrastructure;

/// <summary>
/// Registro de EF Core/Postgres, auth JWT customizado e serviços de infraestrutura.
/// Chamado a partir de FinancialMcp.Api (que também chama builder.AddNpgsqlDbContext
/// via integração Aspire para pegar a connection string por service discovery).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Parser de extrato CSV usado por ImportStatementCommandHandler.
        services.AddScoped<IStatementCsvParser, StatementCsvParser>();

        return services;
    }
}
