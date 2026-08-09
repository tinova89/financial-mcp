using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Infrastructure.Persistence;

/// <summary>
/// DbContext único do projeto. Aplica o global query filter de soft delete em
/// todas as entidades derivadas de BaseEntity (ver CLAUDE.md > Persistência > Soft delete).
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Transacao> Transactions => Set<Transacao>();
    public DbSet<Conta> Accounts => Set<Conta>();
    public DbSet<Cartao> Cards => Set<Cartao>();
    public DbSet<MetaOrcamento> BudgetGoals => Set<MetaOrcamento>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filter de soft delete — queries administrativas explícitas
        // podem ignorar via IgnoreQueryFilters().
        modelBuilder.Entity<Transacao>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Conta>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Cartao>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<MetaOrcamento>().HasQueryFilter(m => !m.IsDeleted);

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
