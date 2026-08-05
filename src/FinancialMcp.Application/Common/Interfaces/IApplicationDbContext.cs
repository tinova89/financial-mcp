using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>
/// Abstração do DbContext exposta à Application, para que handlers do MediatR
/// não dependam diretamente de FinancialMcp.Infrastructure (implementado lá).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Transacao> Transacoes { get; }
    DbSet<Conta> Contas { get; }
    DbSet<Cartao> Cartoes { get; }
    DbSet<MetaOrcamento> MetasOrcamento { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
