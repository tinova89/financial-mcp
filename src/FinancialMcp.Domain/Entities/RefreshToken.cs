using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Refresh token opaco do provedor JWT customizado. Tabela própria — nunca
/// reaproveitar a tabela de usuários (ver CLAUDE.md > Autenticação JWT customizado).
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = default!; // nunca armazenar o token em texto puro
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }   // rotação a cada uso

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
