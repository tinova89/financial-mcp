using FluentValidation;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Valida os campos de acordo com o formato de origem do extrato (separador ";",
/// datas dd/mm/aaaa já parseadas em DateOnly, decimal com ponto já parseado em
/// decimal) antes de persistir — ver CLAUDE.md > MCP.
/// </summary>
public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    private static readonly string[] OrigensValidas = ["ContaCorrente", "CartaoCredito"];
    private static readonly string[] TiposValidos = ["Despesa", "Receita", "Transferencia", "Pagamento"];

    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Origem).Must(o => OrigensValidas.Contains(o))
            .WithMessage($"Origem deve ser uma de: {string.Join(", ", OrigensValidas)}.");

        RuleFor(x => x.Tipo).Must(t => TiposValidos.Contains(t))
            .WithMessage($"Tipo deve ser um de: {string.Join(", ", TiposValidos)}.");

        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(500);

        RuleFor(x => x.Valor).NotEqual(0m);

        RuleFor(x => x.CategoriaBruta).NotEmpty();

        RuleFor(x => x.DataPrevista).NotEqual(default(DateOnly));

        // Regras específicas de Cartão de Crédito.
        When(x => x.Origem == "CartaoCredito", () =>
        {
            RuleFor(x => x.CartaoId).NotNull()
                .WithMessage("CartaoId é obrigatório para transações de Cartão de Crédito.");

            RuleFor(x => x.VencimentoFatura).NotNull()
                .WithMessage("VencimentoFatura é obrigatório para transações de Cartão de Crédito.");

            When(x => x.Repeticao == "Parcelado", () =>
            {
                RuleFor(x => x.ParcelaAtual).NotNull().GreaterThan(0);
                RuleFor(x => x.ParcelaTotal).NotNull()
                    .Must((cmd, total) => total is null || cmd.ParcelaAtual is null || total >= cmd.ParcelaAtual)
                    .WithMessage("ParcelaTotal deve ser maior ou igual a ParcelaAtual.");
            });
        });

        // Regras específicas de Conta Corrente.
        When(x => x.Origem == "ContaCorrente", () =>
        {
            RuleFor(x => x.ContaId).NotNull()
                .WithMessage("ContaId é obrigatório para transações de Conta Corrente.");
        });
    }
}
