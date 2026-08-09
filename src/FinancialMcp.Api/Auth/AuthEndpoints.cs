using System.Security.Cryptography;
using System.Text;
using FinancialMcp.Infrastructure.Persistence;
using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FinancialMcp.Infrastructure.Interfaces;

namespace FinancialMcp.Api.Auth;

/// <summary>
/// Dedicated token issuance endpoint (POST /auth/token), per CLAUDE.md >
/// Authentication (Custom JWT) > Issuance. Credential validation here is a
/// simple placeholder — replace with real username/password verification.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/token", async (
            TokenRequest request,
            IJwtTokenService tokenService,
            ApplicationDbContext db,
            CancellationToken cancellationToken) =>
        {
            // TODO: validate real credentials (username/password) before issuing the token.
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.Unauthorized();
            }

            var userId = DeriveUserId(request.Username);
            var scopes = new[] { "transactions:read", "transactions:write", "budget:read" };

            var accessToken = tokenService.GenerateAccessToken(userId, scopes);
            var (refreshToken, expiresAt) = tokenService.GenerateRefreshToken();

            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = Hash(refreshToken),
                ExpiresAt = expiresAt
            });

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new TokenResponse(accessToken, refreshToken, expiresAt));
        })
        .WithName("IssueToken")
        .AllowAnonymous();

        group.MapPost("/refresh", async (
            RefreshRequest request,
            IJwtTokenService tokenService,
            ApplicationDbContext db,
            CancellationToken cancellationToken) =>
        {
            var hash = Hash(request.RefreshToken);

            var existing = await db.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == hash, cancellationToken);

            if (existing is null || !existing.IsActive)
            {
                return Results.Unauthorized();
            }

            // Rotation: revokes the used token and issues a new one (see CLAUDE.md > Authentication > Refresh).
            existing.RevokedAt = DateTimeOffset.UtcNow;

            var (newRefreshToken, expiresAt) = tokenService.GenerateRefreshToken();
            existing.ReplacedByTokenHash = Hash(newRefreshToken);

            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = existing.UserId,
                TokenHash = Hash(newRefreshToken),
                ExpiresAt = expiresAt
            });

            var accessToken = tokenService.GenerateAccessToken(
                existing.UserId, ["transactions:read", "transactions:write", "budget:read"]);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new TokenResponse(accessToken, newRefreshToken, expiresAt));
        })
        .WithName("RefreshToken")
        .AllowAnonymous();

        return app;
    }

    private static Guid DeriveUserId(string username)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(username.ToLowerInvariant()));
        return new Guid(bytes[..16]);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed record TokenRequest(string Username, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
