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
/// (statement CSV parser, current-request group accessor). Called from FinancialMcp.Api.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Statement CSV parser used by ImportStatementCommandHandler.
        services.AddScoped<IStatementCsvParser, StatementCsvParser>();

        // Current-request account group (X-Account-Group header) — see CurrentGroupService.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentGroupService, CurrentGroupService>();

        return services;
    }
}
