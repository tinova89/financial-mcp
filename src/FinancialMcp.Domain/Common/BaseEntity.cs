namespace FinancialMcp.Domain.Common;

/// <summary>
/// Base for entities with identity and soft delete (see CLAUDE.md > Persistence > Soft delete).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete: never physically remove a transaction. The DbContext applies
    /// a global query filter that excludes records with IsDeleted = true by default.
    /// </summary>
    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
