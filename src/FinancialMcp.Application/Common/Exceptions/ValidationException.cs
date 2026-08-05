using FluentValidation.Results;

namespace FinancialMcp.Application.Common.Exceptions;

/// <summary>
/// Lançada pelo ValidationBehavior quando um ou mais IValidator&lt;TRequest&gt; falham.
/// Mapeada para erro MCP/HTTP apropriado na camada de API (ver CLAUDE.md > Padrão Mediator).
/// </summary>
public sealed class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("Um ou mais erros de validação ocorreram.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
