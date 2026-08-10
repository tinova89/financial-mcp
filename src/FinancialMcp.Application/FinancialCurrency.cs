namespace FinancialMcp.Application;

/// <summary>
/// Represents a financial currency with its ISO code, display name, image resource, and asset file name.
/// </summary>
/// <remarks>Includes an ImageBuffer property for binary image data. Provides predefined currencies via All and
/// lookup via GetCurrency (throws ArgumentOutOfRangeException for unknown codes). Use WithImageBuffer to create a copy
/// with an updated image buffer.</remarks>
/// <param name="CurrencyCode">ISO 4217 three-letter currency code (e.g., "USD").</param>
/// <param name="DisplayName">Human-readable name used for display (e.g., "Dollar").</param>
/// <param name="ImageUrl">Relative URL or path to the currency image resource (e.g., "/images/dollar.svg").</param>
/// <param name="AssetFileName">File name of the associated asset resource (e.g., "dollar.svg").</param>
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
