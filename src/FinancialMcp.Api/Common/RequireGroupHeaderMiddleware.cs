using System.Net;
using FinancialMcp.Infrastructure;

namespace FinancialMcp.Api.Common;

/// <summary>
/// Enforces that every business request (REST financial API + MCP) includes the
/// X-Account-Group header, identifying which group's accounts the request may operate
/// on (e.g. "HOME", "SOM" — see Account.Group). Infra endpoints (health checks, API
/// docs) are exempt since they aren't scoped to any group.
/// </summary>
public sealed class RequireGroupHeaderMiddleware(RequestDelegate next)
{
    private static readonly string[] ExemptPathPrefixes = ["/health", "/alive", "/openapi", "/scalar"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isExempt = ExemptPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (!isExempt)
        {
            var groupHeader = context.Request.Headers[CurrentGroupService.GroupHeaderName].ToString();

            if (string.IsNullOrWhiteSpace(groupHeader))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Cabeçalho obrigatório ausente",
                    status = (int)HttpStatusCode.BadRequest,
                    detail = $"O cabeçalho '{CurrentGroupService.GroupHeaderName}' é obrigatório em todas as requisições (ex.: \"HOME\", \"SOM\")."
                });

                return;
            }
        }

        await next(context);
    }
}
