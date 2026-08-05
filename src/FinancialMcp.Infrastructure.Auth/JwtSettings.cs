namespace FinancialMcp.Infrastructure.Auth;

/// <summary>
/// Configuração do provedor JWT customizado. Chave/segredos nunca em appsettings.json
/// versionado — usar dotnet user-secrets localmente e secret manager em produção
/// (ver CLAUDE.md > Autenticação JWT customizado > Segredos).
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    /// <summary>Chave HMAC (mínimo 256 bits) via configuração/secret — nunca hardcoded.</summary>
    public required string SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}
