using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Infrastructure.Auth;
using FinancialMcp.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FinancialMcp.Infrastructure;

/// <summary>
/// Registration of the custom JWT auth services (token issuance/validation,
/// current-user accessor) and the ASP.NET Core auth middleware. Called from FinancialMcp.Api.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Custom JWT auth.
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var signingKey = jwtSection["SigningKey"] ?? string.Empty;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                // The same auth pipeline is reused by the MCP host (see CLAUDE.md > MCP > Auth):
                // the token is read from the standard Authorization header both by REST routes and
                // by the MCP endpoint, which runs on the same ASP.NET Core host.
            });

        services.AddAuthorization();
        return services;
    }
}
