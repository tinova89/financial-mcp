namespace FinancialMcp.Application;

public sealed record FinancialCurrency(
    string CurrencyCode,
    string DisplayName,
    string ImageUrl,
    string AssetFileName)
{
    public byte[] ImageBuffer { get; init; } = [];

    // https://pt.iban.com/currency-codes
    #region Currencies
    public static FinancialCurrency RealBrasileiro => new("BRL", "Itaú", "/images/real.svg", "real.svg");
    public static FinancialCurrency DollarAmericano => new("USD", "Dollar",  "/images/dollar.svg", "dollar.svg");
    public static FinancialCurrency Bitcoin => new("BTC", "Bitcoin", "/images/bitcoin.svg", "bitcoin.svg");
    #endregion

    public static IReadOnlyList<FinancialCurrency> All => [RealBrasileiro, DollarAmericano, Bitcoin];

    public static FinancialCurrency GetCurrency(string code) =>
        All.FirstOrDefault(b => b.CurrencyCode == code)
        ?? throw new ArgumentOutOfRangeException(nameof(CurrencyCode), code, "Unknown currency code.");

    public FinancialCurrency WithImageBuffer(byte[] imageBuffer) => this with { ImageBuffer = imageBuffer };
}
