namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>
/// The account group (e.g. "HOME", "SOM") for the current request, read from the
/// X-Account-Group HTTP header. Its presence is enforced by RequireGroupHeaderMiddleware
/// before any handler runs, so command handlers can treat a null value as an unexpected
/// invariant violation rather than a normal case to handle gracefully.
/// </summary>
public interface ICurrentGroupService
{
    string? Group { get; }
}
