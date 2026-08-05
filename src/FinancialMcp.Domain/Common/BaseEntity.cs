namespace FinancialMcp.Domain.Common;

/// <summary>
/// Base para entidades com identidade e soft delete (ver CLAUDE.md > Persistência > Soft delete).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete: nunca remover fisicamente uma transação. O DbContext aplica
    /// um global query filter que exclui registros com IsDeleted = true por padrão.
    /// </summary>
    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
