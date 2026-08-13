using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>
/// DbContext abstraction exposed to the Application layer, so that MediatR handlers
/// don't depend directly on FinancialMcp.Infrastructure (implemented there).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Transaction> Transactions { get; }
    DbSet<TransactionCategory> TransactionCategories { get; }
    DbSet<Account> Accounts { get; }
    DbSet<CreditCard> CreditCards { get; }
    DbSet<BudgetGoal> BudgetGoals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
