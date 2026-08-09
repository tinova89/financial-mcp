using FluentValidation.Results;

namespace FinancialMcp.Application.Common.Exceptions;

/// <summary>
/// Thrown by ValidationBehavior when one or more IValidator&lt;TRequest&gt; fail.
/// Mapped to the appropriate MCP/HTTP error in the API layer (see CLAUDE.md > Mediator Pattern).
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
