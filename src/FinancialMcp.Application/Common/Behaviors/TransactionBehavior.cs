using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinancialMcp.Application.Common.Behaviors;

/// <summary>
/// Marks requests that need an explicit database transaction (commands that write
/// via EF Core). See CLAUDE.md > Mediator Pattern > Pipeline behaviors, item 3.
/// </summary>
public interface ITransactionalRequest;

/// <summary>
/// 3rd pipeline behavior (only for requests implementing ITransactionalRequest):
/// opens a database transaction, runs the handler, commits/rolls back.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    DbContext dbContext,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
        {
            return await next();
        }

        if (dbContext.Database.CurrentTransaction is not null)
        {
            // A transaction is already in progress (e.g. nested call) — don't open another one.
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        // Npgsql's retrying execution strategy forbids user-managed transactions started
        // outside of it (it needs to retry the whole begin/commit unit on a transient
        // failure). Run the transaction through the strategy instead of BeginTransactionAsync
        // directly — see https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency.
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogDebug("Transação de banco commitada para {RequestName}", requestName);

                return response;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning("Transação de banco revertida (rollback) para {RequestName}", requestName);

                throw;
            }
        });
    }
}
