namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>Identity of the caller authenticated via JWT (REST or MCP), populated by the auth pipeline.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool HasScope(string scope);
}
