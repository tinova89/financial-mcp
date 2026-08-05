namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>Identidade do chamador autenticado via JWT (REST ou MCP), populada pelo pipeline de auth.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool HasScope(string scope);
}
