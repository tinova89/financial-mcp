using System.Net;
using FinancialMcp.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinancialMcp.Api.Tests;

/// <summary>
/// Teste de integração básico via TestServer/WebApplicationFactory (ver CLAUDE.md
/// > Orientações de Teste > "TestServer + client MCP real para testes de
/// integração das ferramentas expostas"). Serve de esqueleto para os testes de
/// integração das tools MCP (list_transactions, create_transaction, etc.).
/// </summary>
public class HealthCheckTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Alive_endpoint_deve_responder_ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
