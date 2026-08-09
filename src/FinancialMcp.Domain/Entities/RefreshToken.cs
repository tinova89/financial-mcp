using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Opaque refresh token from the custom JWT provider. Its own table — never
/// reuse the users table (see CLAUDE.md > Authentication Custom JWT).
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!; // never store the token in plain text
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }   // rotated on each use

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
