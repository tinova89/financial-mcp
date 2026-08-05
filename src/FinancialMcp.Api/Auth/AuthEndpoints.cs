using System.Security.Cryptography;
using System.Text;
using FinancialMcp.Infrastructure.Persistence;
using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FinancialMcp.Infrastructure.Interfaces;

namespace FinancialMcp.Api.Auth;

/// <summary>
/// Endpoint próprio de emissão de token (POST /auth/token), conforme CLAUDE.md >
/// Autenticação (JWT customizado) > Emissão. Validação de credenciais aqui é um
/// placeholder simples — substituir por verificação real de usuário/senha.
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
            // TODO: validar credenciais reais (usuário/senha) antes de emitir o token.
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
                UsuarioId = userId,
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

            // Rotação: revoga o token usado e emite um novo (ver CLAUDE.md > Autenticação > Refresh).
            existing.RevokedAt = DateTimeOffset.UtcNow;

            var (newRefreshToken, expiresAt) = tokenService.GenerateRefreshToken();
            existing.ReplacedByTokenHash = Hash(newRefreshToken);

            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = existing.UsuarioId,
                TokenHash = Hash(newRefreshToken),
                ExpiresAt = expiresAt
            });

            var accessToken = tokenService.GenerateAccessToken(
                existing.UsuarioId, ["transactions:read", "transactions:write", "budget:read"]);

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
