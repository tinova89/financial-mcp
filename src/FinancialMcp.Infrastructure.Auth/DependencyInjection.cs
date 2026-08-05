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
/// Registro de EF Core/Postgres, auth JWT customizado e serviços de infraestrutura.
/// Chamado a partir de FinancialMcp.Api (que também chama builder.AddNpgsqlDbContext
/// via integração Aspire para pegar a connection string por service discovery).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Auth JWT customizado.
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

                // Mesmo pipeline de auth é reutilizado pelo host MCP (ver CLAUDE.md > MCP > Auth):
                // o token é lido do header Authorization padrão tanto pelas rotas REST quanto pelo
                // endpoint MCP, que roda sobre o mesmo host ASP.NET Core.
            });

        services.AddAuthorization();
        return services;
    }
}
