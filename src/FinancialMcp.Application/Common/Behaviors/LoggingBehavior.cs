using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinancialMcp.Application.Common.Behaviors;

/// <summary>
/// 1st pipeline behavior: logs request/response (no sensitive data) and integrates
/// with Aspire/OpenTelemetry tracing via the ActivitySource "FinancialMcp.Application.MediatR"
/// (see CLAUDE.md > Mediator Pattern > Pipeline behaviors).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("FinancialMcp.Application.MediatR");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = ActivitySource.StartActivity(requestName);

        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Iniciando {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();

            logger.LogInformation(
                "Concluído {RequestName} em {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            logger.LogError(
                ex,
                "Falha em {RequestName} após {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
