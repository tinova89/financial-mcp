using FinancialMcp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FinancialMcp.Infrastructure.Auth;

/// <summary>Implementation of ICurrentUserService from HttpContext (JWT already validated by middleware).</summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User
                .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool HasScope(string scope) =>
        httpContextAccessor.HttpContext?.User.HasClaim("scope", scope) ?? false;
}
