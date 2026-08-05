using System.Reflection;
using FinancialMcp.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialMcp.Application;

/// <summary>Marcador do assembly, usado por RegisterServicesFromAssembly (MediatR/FluentValidation).</summary>
public sealed class AssemblyMarker;

/// <summary>
/// Registro centralizado do MediatR + FluentValidation + pipeline behaviors em
/// FinancialMcp.Application. Referenciado a partir de FinancialMcp.Api — nunca
/// registrar assemblies MediatR diretamente na camada de API (ver CLAUDE.md >
/// Padrão Mediator > Registro).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Ordem do pipeline: Logging -> Validation -> Transaction.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
