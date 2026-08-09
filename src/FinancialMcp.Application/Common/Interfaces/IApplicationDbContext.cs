using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>
/// DbContext abstraction exposed to the Application layer, so that MediatR handlers
/// don't depend directly on FinancialMcp.Infrastructure (implemented there).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Transacao> Transactions { get; }
    DbSet<Conta> Accounts { get; }
    DbSet<Cartao> Cards { get; }
    DbSet<MetaOrcamento> BudgetGoals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
