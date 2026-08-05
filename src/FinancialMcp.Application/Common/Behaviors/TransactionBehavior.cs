using FinancialMcp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FinancialMcp.Application.Common.Behaviors;

/// <summary>
/// Marca requests que precisam de transação de banco explícita (commands que gravam
/// via EF Core). Ver CLAUDE.md > Padrão Mediator > Pipeline behaviors, item 3.
/// </summary>
public interface ITransactionalRequest;

/// <summary>
/// 3º behavior do pipeline (apenas para requests que implementam ITransactionalRequest):
/// abre transação de banco, executa o handler, faz commit/rollback.
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

        var requestName = typeof(TRequest).Name;

        if (dbContext.Database.CurrentTransaction is not null)
        {
            // Já existe uma transação em andamento (ex.: chamada aninhada) — não abrir outra.
            return await next();
        }

        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var response = await next();

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogDebug("Transação de banco commitada para {RequestName}", requestName);

            return response;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning("Transação de banco revertida (rollback) para {RequestName}", requestName);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
