namespace FinancialMcp.Application.Common.Services;

/// <summary>
/// Helper único para "próximo dia útil", reutilizado por GetBalanceProjection e
/// GetBudgetStatus (ver CLAUDE.md > Convenções de Código > Datas). Considera apenas
/// fins de semana — feriados nacionais/locais ficam fora de escopo do MVP.
/// </summary>
public static class BusinessDayHelper
{
    public static DateOnly ProximoDiaUtil(DateOnly data)
    {
        var resultado = data;

        while (resultado.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            resultado = resultado.AddDays(1);
        }

        return resultado;
    }

    public static bool EhDiaUtil(DateOnly data) =>
        data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
