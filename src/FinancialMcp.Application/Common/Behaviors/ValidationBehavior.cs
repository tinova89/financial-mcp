using FluentValidation;
using MediatR;
using ValidationException = FinancialMcp.Application.Common.Exceptions.ValidationException;

namespace FinancialMcp.Application.Common.Behaviors;

/// <summary>
/// 2º behavior do pipeline: executa todos os IValidator&lt;TRequest&gt; (FluentValidation)
/// antes do handler. Lança ValidationException em caso de falha (ver CLAUDE.md > Padrão
/// Mediator > Pipeline behaviors).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
