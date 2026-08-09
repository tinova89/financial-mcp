// FinancialMcp.AppHost / Program.cs
// Local orchestration via .NET Aspire: provisions Postgres, injects the connection
// string into the API via service discovery, and enables the Aspire dashboard.
// See CLAUDE.md > Architecture > Aspire / Persistence (Postgres).

using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Machine-specific overrides (optional). Loaded after appsettings.json and appsettings.{Environment}.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Postgres resource — resource name "financialmcp-postgres", database "financialmcp-db".
// In production this resource is replaced by the real connection string via
// environment/secret configuration (never hardcoded here).
var postgres = builder
    .AddPostgres("financialmcp-postgres")
    .WithDataVolume(isReadOnly: false)
    .WithPgAdmin();

var financialDb = postgres.AddDatabase("financialmcp-db");

// API: ASP.NET Core Web API + MCP Server host, referencing Postgres via
// service discovery (never a hardcoded URL/connection string in the API project).
var api = builder
    .AddProject<Projects.FinancialMcp_Api>("financialmcp-api")
    .WithReference(financialDb)
    .WaitFor(financialDb)
    .WithExternalHttpEndpoints();

// Example React frontend as an npm resource, if/when it exists at /src/financialmcp-web.
// var web = builder.AddNpmApp("financialmcp-web", "../../src/financialmcp-web")
//     .WithReference(api)
//     .WaitFor(api)
//     .WithHttpEndpoint(env: "PORT")
//     .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
