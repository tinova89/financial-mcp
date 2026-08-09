namespace FinancialMcp.Infrastructure.Auth;

/// <summary>
/// Configuration for the custom JWT provider. Keys/secrets never in versioned
/// appsettings.json — use dotnet user-secrets locally and a secret manager in
/// production (see CLAUDE.md > Authentication Custom JWT > Secrets).
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    /// <summary>HMAC key (minimum 256 bits) via configuration/secret — never hardcoded.</summary>
    public required string SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}
