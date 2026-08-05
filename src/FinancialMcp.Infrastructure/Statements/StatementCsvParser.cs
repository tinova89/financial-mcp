using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FinancialMcp.Application.Statements.ImportStatement;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;

namespace FinancialMcp.Infrastructure.Statements;

/// <summary>
/// Parser concreto do formato de extrato: separador ";", datas "dd/mm/aaaa",
/// decimal com ponto (ver CLAUDE.md > MCP e Convenções de Código). Linhas
/// inválidas geram um aviso e são puladas — nunca lançam exceção que aborta o
/// lote inteiro, para permitir importação parcial com feedback ao chamador.
/// </summary>
public sealed class StatementCsvParser : IStatementCsvParser
{
    public IReadOnlyList<Transacao> Parse(
        string csvContent, string origem, Guid? contaId, Guid? cartaoId, out IReadOnlyList<string> avisos)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        var transacoes = new List<Transacao>();
        var listaAvisos = new List<string>();

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        var linha = 1;

        while (csv.Read())
        {
            linha++;

            try
            {
                var transacao = MapearLinha(csv, origem, contaId, cartaoId);
                transacoes.Add(transacao);
            }
            catch (Exception ex)
            {
                listaAvisos.Add($"Linha {linha}: {ex.Message}");
            }
        }

        avisos = listaAvisos;
        return transacoes;
    }

    private static Transacao MapearLinha(CsvReader csv, string origem, Guid? contaId, Guid? cartaoId)
    {
        var origemEnum = Enum.Parse<OrigemTransacao>(origem);

        var valorBruto = csv.GetField("Valor") ?? throw new FormatException("Campo 'Valor' ausente.");
        var valor = decimal.Parse(valorBruto, NumberStyles.Number, CultureInfo.InvariantCulture);

        var tipoBruto = csv.GetField("Tipo") ?? throw new FormatException("Campo 'Tipo' ausente.");
        var statusBruto = csv.GetField("Status") ?? throw new FormatException("Campo 'Status' ausente.");
        var categoria = csv.GetField("Categoria") ?? "Sem categoria";
        var descricao = csv.GetField("Descrição") ?? csv.GetField("Descricao") ?? string.Empty;

        var dataPrevista = ParseDataDdMmAaaa(csv.GetField("Data prevista"))
            ?? throw new FormatException("Campo 'Data prevista' inválido ou ausente.");

        var transacao = new Transacao
        {
            Origem = origemEnum,
            Tipo = Enum.Parse<TipoTransacao>(tipoBruto, ignoreCase: true),
            Status = Enum.Parse<StatusTransacao>(statusBruto, ignoreCase: true),
            Descricao = descricao,
            Valor = valor,
            CategoriaBruta = categoria,
            DataPrevista = dataPrevista,
            DataEfetiva = ParseDataDdMmAaaa(csv.GetField("Data efetiva")),
            ContaId = origemEnum == OrigemTransacao.ContaCorrente ? contaId : null,
            CartaoId = origemEnum == OrigemTransacao.CartaoCredito ? cartaoId : null
        };

        if (origemEnum == OrigemTransacao.ContaCorrente)
        {
            transacao.DataConciliado = ParseDataDdMmAaaa(csv.GetField("Data Conciliado"));
        }
        else
        {
            transacao.VencimentoFatura = ParseDataDdMmAaaa(csv.GetField("Venc. Fatura"));

            var repeticaoBruta = csv.GetField("Repetição") ?? csv.GetField("Repeticao");
            transacao.Repeticao = repeticaoBruta switch
            {
                "Parcelado" => TipoRepeticao.Parcelado,
                "Fixo Mês" or "Fixo Mes" => TipoRepeticao.FixoMes,
                _ => TipoRepeticao.Nenhuma
            };

            if (transacao.Repeticao == TipoRepeticao.Parcelado)
            {
                var parcela = csv.GetField("Parcela Atual");
                var totalParcelas = csv.GetField("Parcela Total");

                if (parcela is not null && totalParcelas is not null)
                {
                    transacao.ParcelaAtual = int.Parse(parcela, CultureInfo.InvariantCulture);
                    transacao.ParcelaTotal = int.Parse(totalParcelas, CultureInfo.InvariantCulture);
                }
            }
        }

        return transacao;
    }

    private static DateOnly? ParseDataDdMmAaaa(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return DateOnly.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : null;
    }
}
