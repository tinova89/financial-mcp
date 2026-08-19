using FinancialMcp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FinancialMcp.Infrastructure;

/// <summary>
/// Implementation of ICurrentGroupService from HttpContext. The header's presence is
/// enforced by FinancialMcp.Api.Common.RequireGroupHeaderMiddleware before any handler
/// runs — GroupHeaderName is the single source of truth for the header name, referenced
/// by that middleware too, so the two never drift apart.
/// </summary>
public sealed class CurrentGroupService(IHttpContextAccessor httpContextAccessor) : ICurrentGroupService
{
    public const string GroupHeaderName = "X-Account-Group";

    public string? Group
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers[GroupHeaderName].ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
