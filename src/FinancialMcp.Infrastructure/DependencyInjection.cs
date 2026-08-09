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
/// Registration of infrastructure services not tied to persistence or auth
/// (currently just the statement CSV parser). Called from FinancialMcp.Api.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Statement CSV parser used by ImportStatementCommandHandler.
        services.AddScoped<IStatementCsvParser, StatementCsvParser>();

        return services;
    }
}
