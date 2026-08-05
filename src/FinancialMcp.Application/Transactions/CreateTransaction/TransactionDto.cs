namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>DTO de resposta — nunca expor a entidade de domínio Transacao diretamente (ver CLAUDE.md > DTOs).</summary>
public sealed record TransactionDto(
    Guid Id,
    string Origem,
    string Tipo,
    string Status,
    string Descricao,
    decimal Valor,
    string CategoriaBruta,
    DateOnly DataPrevista,
    DateOnly? DataEfetiva,
    DateOnly? DataConciliado,
    DateOnly? VencimentoFatura,
    string? Repeticao,
    int? ParcelaAtual,
    int? ParcelaTotal,
    Guid? ContaId,
    Guid? CartaoId);
