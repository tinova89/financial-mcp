using FinancialMcp.Application.Common.Services;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Cobre CLAUDE.md > Orientações de Teste > "Ciclo de fechamento/vencimento de
/// fatura, incluindo virada para o próximo dia útil em fins de semana".
/// </summary>
public class BusinessDayHelperTests
{
    [Theory]
    [InlineData(2026, 8, 8, 2026, 8, 10)]  // sábado -> segunda
    [InlineData(2026, 8, 9, 2026, 8, 10)]  // domingo -> segunda
    [InlineData(2026, 8, 10, 2026, 8, 10)] // segunda permanece
    public void ProximoDiaUtil_deve_virar_fim_de_semana_para_segunda(
        int ano, int mes, int dia, int anoEsperado, int mesEsperado, int diaEsperado)
    {
        var resultado = BusinessDayHelper.NextBusinessDay(new DateOnly(ano, mes, dia));

        resultado.Should().Be(new DateOnly(anoEsperado, mesEsperado, diaEsperado));
    }
}
