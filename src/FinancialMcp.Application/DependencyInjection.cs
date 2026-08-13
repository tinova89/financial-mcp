using System.Reflection;
using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Common.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialMcp.Application;

/// <summary>Assembly marker, used by RegisterServicesFromAssembly (MediatR/FluentValidation).</summary>
public sealed class AssemblyMarker;

/// <summary>
/// Centralized registration of MediatR + FluentValidation + pipeline behaviors in
/// FinancialMcp.Application. Referenced from FinancialMcp.Api — never
/// register MediatR assemblies directly in the API layer (see CLAUDE.md >
/// Mediator Pattern > Registration).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline order: Logging -> Validation -> Transaction.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<ITransactionCategoryResolver, TransactionCategoryResolver>();

        return services;
    }
}
