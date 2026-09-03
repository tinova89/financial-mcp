namespace FinancialMcp.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated (or anonymous) but lacks the JWT scope
/// required for the operation — e.g. calling <c>approve_revision</c> without the
/// <c>approval</c> scope. Mapped to HTTP/MCP <c>403 Forbidden</c> by
/// <c>ExceptionHandlingMiddleware</c>, before the request ever reaches a MediatR handler
/// (see CLAUDE.md &gt; Authentication (Custom JWT) &gt; Scopes).
/// </summary>
public sealed class ForbiddenException(string requiredScope)
    : Exception($"Escopo obrigatório ausente: \"{requiredScope}\". A operação foi negada (403).")
{
    public string RequiredScope { get; } = requiredScope;
}
