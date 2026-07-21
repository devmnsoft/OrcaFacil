# OrçaFácil

Reescrita estrutural do OrçaFácil em ASP.NET Core 10, PostgreSQL, Clean Architecture e DDD.

## Projetos

- `src/OrcaFacil.Domain`: entidades, enums, value objects e regras de domínio.
- `src/OrcaFacil.Shared`: resultados e constantes compartilhadas.
- `src/OrcaFacil.Application`: abstrações, DTOs, comandos, validadores e casos de uso.
- `src/OrcaFacil.Persistence`: EF Core, Dapper, repositórios, auditoria e mapeamentos.
- `src/OrcaFacil.Infrastructure`: middleware, usuário corrente, senha e PDF QuestPDF.
- `src/OrcaFacil.Api`: API REST.
- `src/OrcaFacil.Web`: app web Razor.
- `tests/OrcaFacil.UnitTests`: testes unitários.

## Como rodar localmente

```bash
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln
dotnet run --project src/OrcaFacil.Web
```

## Docker

Copie `.env.example` para `.env`, preencha os segredos e execute:

```bash
docker compose up -d
```

## Migrations

```bash
dotnet ef migrations add InitialCreate --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

## Testes

```bash
dotnet test OrcaFacil.sln
```

## Publicação IIS

Execute `publish-iis.bat`. O script deve restaurar, compilar, testar e publicar o projeto web.

## Status atual

A base foi reorganizada para separar entidades, enums, value objects, abstrações, DTOs, comandos, validadores, middleware e PDF. O ambiente desta revisão não possui `dotnet` instalado; portanto, restore/build/test precisam ser confirmados em máquina com SDK .NET 10.
