namespace FinancialMcp.Application;

/// <summary>
/// Represents a financial institution with its bank code, display name, image location, asset file name, and optional
/// in-memory image buffer.
/// </summary>
/// <remarks>Includes predefined instances for common banks and helper members to enumerate all banks and retrieve
/// an instance by code.</remarks>
/// <param name="BankCode">Bank code assigned by banking authorities; a numeric string that identifies the institution. COMPE code</param>
/// <param name="Name">Display name of the bank.</param>
/// <param name="ImageUrl">Relative or absolute URL to the bank's image resource.</param>
/// <param name="AssetFileName">File name of the bundled image asset for the bank.</param>
public sealed record FinancialBank(
    string BankCode,
    string Name,
    string ImageUrl,
    string AssetFileName)
{
    public byte[] ImageBuffer { get; init; } = [];

    // https://www.codigobanco.com.br/
    #region Banks
    public static FinancialBank Itau => new("341", "Itaú", "/images/itau.svg", "itau.svg");
    public static FinancialBank Nubank => new("260", "Nubank",  "/images/nubank.svg", "nubank.svg");
    public static FinancialBank Inter => new("077", "Inter", "/images/inter.svg", "inter.svg");
    public static FinancialBank Wallet => new("307", "Wallet", "/images/wallet.svg", "wallet.svg");
    #endregion

    public static IReadOnlyList<FinancialBank> All => [Itau, Nubank, Inter, Wallet];

    public static FinancialBank GetBank(string code) =>
        All.FirstOrDefault(b => b.BankCode == code)
        ?? throw new ArgumentOutOfRangeException(nameof(BankCode), code, "Unknown bank code.");

    public FinancialBank WithImageBuffer(byte[] imageBuffer) => this with { ImageBuffer = imageBuffer };
}
