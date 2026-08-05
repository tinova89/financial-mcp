// FinancialMcp.AppHost / Program.cs
// Orquestração local via .NET Aspire: provisiona Postgres, injeta a connection
// string por service discovery na API e habilita o dashboard do Aspire.
// Ver CLAUDE.md > Arquitetura > Aspire / Persistência (Postgres).

var builder = DistributedApplication.CreateBuilder(args);

// Recurso Postgres — nome do recurso "financialmcp-postgres", banco "financialmcp-db".
// Em produção esse recurso é substituído pela connection string real via
// configuração de ambiente/secret (nunca hardcoded aqui).
var postgres = builder
    .AddPostgres("financialmcp-postgres")
    .WithDataVolume(isReadOnly: false)
    .WithPgAdmin();

var financialDb = postgres.AddDatabase("financialmcp-db");

// API: ASP.NET Core Web API + MCP Server host, referenciando o Postgres via
// service discovery (nunca URL/connection string hardcoded no projeto da API).
var api = builder
    .AddProject<Projects.FinancialMcp_Api>("financialmcp-api")
    .WithReference(financialDb)
    .WaitFor(financialDb)
    .WithExternalHttpEndpoints();

// Exemplo de frontend React como recurso npm, se/quando existir em /src/financialmcp-web.
// var web = builder.AddNpmApp("financialmcp-web", "../../src/financialmcp-web")
//     .WithReference(api)
//     .WaitFor(api)
//     .WithHttpEndpoint(env: "PORT")
//     .WithExternalHttpEndpoints();

builder.Build().Run();
