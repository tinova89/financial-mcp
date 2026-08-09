using System.Net;
using FinancialMcp.Application.Common.Exceptions;
using ValidationException = FinancialMcp.Application.Common.Exceptions.ValidationException;

namespace FinancialMcp.Api.Common;

/// <summary>
/// Maps exceptions thrown by MediatR handlers (ValidationBehavior,
/// NotFoundException, ConfirmationRequiredException) to the appropriate
/// HTTP/MCP responses (see CLAUDE.md > Mediator Pattern > ValidationBehavior).
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Erro de validação", ex.Errors);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ConfirmationRequiredException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.PreconditionRequired, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Erro interno inesperado.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, HttpStatusCode statusCode, string title, object? errors = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            title,
            status = (int)statusCode,
            errors
        });
    }
}
