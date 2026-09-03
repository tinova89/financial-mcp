using Microsoft.AspNetCore.Authorization;

namespace FinancialMcp.Api.Auth;

/// <summary>
/// The JWT <c>scope</c> claim values issued by <c>POST /auth/token</c> and the
/// authorization-policy names built from them (see CLAUDE.md &gt; Authentication (Custom JWT)
/// &gt; Scopes). Deciding <i>who</i> is granted a scope (role/account policy) is out of scope
/// for Card #15 — only the claim + its enforcement point are defined here.
/// </summary>
public static class AuthScopes
{
    public const string TransactionsRead = "transactions:read";
    public const string TransactionsWrite = "transactions:write";
    public const string BudgetRead = "budget:read";

    /// <summary>
    /// Required to call <c>approve_revision</c>. A caller without this scope is rejected with
    /// <c>403</c> at the MCP tool level, before the request reaches MediatR.
    /// </summary>
    public const string Approval = "approval";

    /// <summary>Every scope granted to a standard client token today.</summary>
    public static readonly string[] Default =
    [
        TransactionsRead, TransactionsWrite, BudgetRead, Approval
    ];

    /// <summary>Authorization-policy name enforcing <see cref="Approval"/>.</summary>
    public const string ApprovalPolicy = "ApprovalScope";

    /// <summary>
    /// Registers the <see cref="ApprovalPolicy"/> policy (a <c>scope</c> claim containing
    /// <see cref="Approval"/>). The policy is available for endpoint-level
    /// <c>RequireAuthorization("ApprovalScope")</c> once the global auth pipeline is enabled;
    /// <c>approve_revision</c>, being a reflection-registered MCP tool with no endpoint
    /// metadata of its own, additionally checks the same scope inline.
    /// </summary>
    public static IServiceCollection AddApprovalScopePolicy(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(ApprovalPolicy, policy => policy.RequireClaim("scope", Approval));

        return services;
    }
}
