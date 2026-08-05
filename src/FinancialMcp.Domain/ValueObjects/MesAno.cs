namespace FinancialMcp.Domain.ValueObjects;

/// <summary>Chave de agregação mensal ("Mês_Ano") usada por get_budget_status e get_balance_projection.</summary>
public readonly record struct MesAno(int Ano, int Mes)
{
    public static MesAno FromDate(DateOnly data) => new(data.Year, data.Month);

    public MesAno AdicionarMeses(int meses)
    {
        var data = new DateOnly(Ano, Mes, 1).AddMonths(meses);
        return new MesAno(data.Year, data.Month);
    }

    public override string ToString() => $"{Mes:D2}/{Ano:D4}";
}
