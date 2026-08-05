using FinancialMcp.Domain.Common;

namespace FinancialMcp.Domain.Entities;

/// <summary>Cartão de crédito (Nubank, Bradesco, Sofisa, e futuros), vinculado a uma Conta de pagamento.</summary>
public class Cartao : BaseEntity
{
    public string Nome { get; set; } = default!;         // ex.: "Nubank", "Bradesco", "Sofisa"
    public byte DiaFechamento { get; set; }               // dia do mês em que a fatura fecha
    public byte DiaVencimento { get; set; }                // dia do mês em que a fatura vence

    public Guid ContaId { get; set; }                      // Conta debitada no pagamento da fatura
    public Conta Conta { get; set; } = default!;

    public ICollection<Transacao> Transacoes { get; set; } = new List<Transacao>();
}
