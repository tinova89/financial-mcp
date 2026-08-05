using System.Security.Claims;

namespace FinancialMcp.Infrastructure.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, IEnumerable<string> scopes);

    (string Token, DateTimeOffset ExpiresAt) GenerateRefreshToken();

    ClaimsPrincipal? ValidateAccessToken(string token);
}
