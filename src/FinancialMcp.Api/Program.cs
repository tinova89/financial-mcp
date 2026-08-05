using FinancialMcp.Api.Auth;
using FinancialMcp.Api.Common;
using FinancialMcp.Application;
using FinancialMcp.Infrastructure;
using FinancialMcp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Defaults compartilhados do Aspire: OpenTelemetry, health checks, resiliência
// (ver CLAUDE.md > Arquitetura > Aspire).
builder.AddServiceDefaults();

// Postgres via integração Aspire: a connection string do recurso "financialmcp-db"
// é injetada automaticamente por service discovery (nunca hardcoded aqui).
builder.AddNpgsqlDbContext<ApplicationDbContext>("financialmcp-db");

// Camadas da aplicação (ver CLAUDE.md > Padrão Mediator > Registro e > Autenticação).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Host MCP sobre o mesmo processo/porta da API, autenticado via JWT bearer
// (ver CLAUDE.md > MCP > Auth) e com as tools registradas por reflexão.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapDefaultEndpoints(); // "/health", "/alive" — ver ServiceDefaults.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

// Endpoint MCP autenticado por JWT bearer — mesmo esquema/pipeline da REST API.
app.MapMcp("/mcp").RequireAuthorization();

await app.RunAsync();

// Exposto para o Aspire AppHost referenciar via AddProject<Projects.FinancialMcp_Api>.
namespace FinancialMcp.Api
{
    public partial class Program;
}
