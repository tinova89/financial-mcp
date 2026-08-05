using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>Conta corrente vinculada a um banco (extrato de CC).</summary>
public class Conta : BaseEntity
{
    public string Nome { get; set; } = default!;
    public string Banco { get; set; } = default!;

    public ICollection<Transacao> Transacoes { get; set; } = new List<Transacao>();
    public ICollection<Cartao> Cartoes { get; set; } = new List<Cartao>();
}
