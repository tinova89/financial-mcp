using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Infrastructure.Persistence;

/// <summary>
/// The project's single DbContext. Applies the global soft-delete query filter to
/// all entities derived from BaseEntity (see CLAUDE.md > Persistence > Soft delete).
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<BudgetGoal> BudgetGoals => Set<BudgetGoal>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global soft-delete query filter — explicit administrative queries can
        // bypass it via IgnoreQueryFilters(). CreditCard shares Account's filter via
        // EF Core TPH inheritance — it must not (and cannot) redeclare its own.
        modelBuilder.Entity<Transaction>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Account>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<BudgetGoal>().HasQueryFilter(m => !m.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
