# FinancialMcp

Solução .NET 10 / Aspire para o **MCP Server financeiro** descrito em `CLAUDE.md`. Este README documenta como abrir, restaurar e rodar a solução; `CLAUDE.md` continua sendo a fonte de verdade para arquitetura e regras de negócio.

## Pré-requisitos

- .NET 10 SDK (ver `global.json`) com suporte a `.slnx` (formato nativo a partir do SDK 9.0.200+).
- Docker (ou outro provedor de containers compatível com Aspire) — usado pelo `AddPostgres` no `AppHost` para subir o Postgres localmente.
- Workload do Aspire: `dotnet workload install aspire` (se ainda não instalado).

## Abrir a solução

```bash
dotnet restore FinancialMcp.slnx
```

> Este scaffold foi gerado sem acesso à internet (ambiente sandbox sem acesso a `nuget.org`), portanto **nenhum pacote foi restaurado nem o build foi validado aqui**. As versões em `Directory.Packages.props` são um ponto de partida — rode `dotnet restore` e ajuste para as versões estáveis mais recentes de Aspire 9.x / EF Core 9.x / `ModelContextProtocol.AspNetCore` disponíveis no seu ambiente.

## Rodar tudo (backend + Aspire dashboard)

```bash
dotnet run --project src/FinancialMcp.AppHost
```

O AppHost provisiona o Postgres (`financialmcp-postgres` / banco `financialmcp-db`), injeta a connection string na API por service discovery, e abre o Aspire dashboard.

## Configurar o segredo do JWT (obrigatório antes do primeiro `dotnet run`)

```bash
cd src/FinancialMcp.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<uma-chave-aleatoria-de-pelo-menos-32-bytes>"
```

Sem esse segredo, `AddJwtBearer` sobe com uma chave vazia (apenas para não quebrar o boot local) — **nunca** rodar assim fora do ambiente de desenvolvimento.

## Migration inicial do EF Core

```bash
dotnet ef migrations add InitialCreate --project src/FinancialMcp.Infrastructure.Persistence --startup-project src/FinancialMcp.Api
dotnet ef database update --project src/FinancialMcp.Infrastructure --startup-project src/FinancialMcp.Api
```

## Rodar testes

```bash
dotnet test FinancialMcp.slnx
```

## Validação feita neste ambiente

Este scaffold foi gerado em um sandbox sem acesso a `nuget.org` (só domínios como `npm`/`pypi`/`github`/`apt` estão liberados), então o restore completo da solução não foi possível aqui. Ainda assim, com o SDK do .NET 10 instalado localmente no sandbox, foi possível validar de verdade:

- **`FinancialMcp.slnx` carrega corretamente** via `dotnet sln list` — a primeira versão gerada tinha um `<Folder Name="/">` aninhado inválido (conflito com a pasta raiz implícita do `.slnx`), que foi **encontrado pelo parser real do SDK e corrigido**.
- **`FinancialMcp.Domain` compila 100% limpo** (`dotnet build`, 0 erros/0 warnings) — é o único projeto sem dependências de pacote externo, então pôde ser restaurado e buildado offline de ponta a ponta.
- Revisão manual dos demais projetos identificou e corrigiu 3 problemas reais de referência que só apareceriam no build completo:
  1. `FinancialMcp.Application.csproj` usava `DbSet<T>`/`DbContext` (em `IApplicationDbContext`, `TransactionBehavior` e vários handlers) sem referenciar o pacote `Microsoft.EntityFrameworkCore` — adicionado.
  2. `FinancialMcp.Infrastructure.csproj` usava `IHttpContextAccessor`/`HttpContext` (`CurrentUserService`) sem `FrameworkReference` para `Microsoft.AspNetCore.App` (necessário em class libraries que não usam o SDK Web) — adicionado.
  3. `FinancialMcp.ServiceDefaults.csproj` usava `Microsoft.AspNetCore.Builder.WebApplication` (`MapDefaultEndpoints`) pelo mesmo motivo — `FrameworkReference` adicionado.

O que **ainda não foi validado** por falta de acesso ao NuGet real: restore completo de `Application`/`Infrastructure`/`Api`/`AppHost`/testes, e portanto a compilação semântica completa desses projetos (uso correto das APIs de MediatR, FluentValidation, EF Core/Npgsql, `ModelContextProtocol.AspNetCore` e Aspire). Assim que você rodar `dotnet restore && dotnet build` com acesso normal à internet, é esperado que apareçam pequenos ajustes de API (principalmente em `ModelContextProtocol.AspNetCore`, que é uma biblioteca ainda em preview e muda com frequência) — mas a arquitetura, os nomes de tipos e a lógica de negócio já foram revisados a fundo.

## O que já está implementado neste scaffold

- Estrutura completa da solução (`.slnx` + todos os projetos de `CLAUDE.md`), com `Directory.Build.props`/`Directory.Packages.props` centralizando target framework e versões de pacote.
- `AppHost` provisionando Postgres e referenciando a Api (`WithReference`/`WaitFor`).
- `ServiceDefaults` com OpenTelemetry, health checks (`/health`, `/alive`) e resiliência HTTP padrão.
- `Domain`: entidades (`Transacao`, `Conta`, `Cartao`, `MetaOrcamento`, `RefreshToken`), enums e os value objects `Categoria` e `MesAno` centralizando as regras de parsing/agregação.
- `Application`: pipeline MediatR completo (Logging → Validation → Transaction), e as 9 features CQRS correspondentes 1:1 às tools MCP do `CLAUDE.md` (`list_transactions`, `get_transaction`, `create_transaction`, `update_transaction`, `delete_transaction`, `confirm_transaction`, `list_categories`, `get_budget_status`, `get_balance_projection`, `import_statement`), incluindo a notification de confirmação.
- `Infrastructure`: `ApplicationDbContext` com soft delete global, mapeamentos EF Core (`numeric`, `date`/`timestamptz`, índices), provedor JWT customizado (emissão, validação, refresh com rotação) e o parser CSV do formato de extrato.
- `Api`: `Program.cs` unificando Aspire + Postgres + Auth JWT + MediatR + host MCP (`WithHttpTransport`), tools MCP como wrappers finos sobre `IMediator`, endpoints `/auth/token` e `/auth/refresh`, e middleware de tratamento de erros mapeando `ValidationException`/`NotFoundException` para respostas HTTP/MCP.
- Testes: exemplos unitários para `Categoria` e `BusinessDayHelper` (Application.Tests) e um teste de integração esqueleto via `WebApplicationFactory` (Api.Tests).

## O que ainda precisa de atenção antes de produção

- Validação real de usuário/senha em `POST /auth/token` (hoje é um placeholder).
- Migration inicial do EF Core (não gerada aqui — requer o SDK instalado).
- Ajuste fino de versões de pacote em `Directory.Packages.props` contra o feed de NuGet real.
- Índices/queries de `get_balance_projection` e `get_budget_status` foram implementados com filtragem em memória para o Mês_Ano de referência (regra que mistura duas colunas de data conforme a origem); revisar performance em bases grandes e considerar mover parte do filtro para SQL via `EF.Functions`.
