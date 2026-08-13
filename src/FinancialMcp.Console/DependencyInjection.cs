using FinancialMcp.Console.CsvTest;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialMcp.Console;

/// <summary>
/// Centralized DI registration for FinancialMcp.Console, mirroring the
/// Add&lt;Layer&gt;(this IServiceCollection) pattern used by the other projects
/// (see FinancialMcp.Application/DependencyInjection.cs and CLAUDE.md > Mediator Pattern).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddConsoleServices(this IServiceCollection services)
    {
        services.AddTransient<CsvTestService>();

        return services;
    }
}
