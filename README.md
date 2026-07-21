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

## MVP ASP.NET Core/PostgreSQL

Esta etapa adiciona a base funcional do MVP backend: autenticação por cookie, serviços de perfil do emitente, numeração única de documentos, criação/edição/duplicação/exclusão lógica, geração de PDF com QuestPDF e marca d'água para plano Free, consultas Dapper para histórico/dashboard, limites Free/Pro e painel SuperAdmin básico.

### Rodar localmente

1. Suba o PostgreSQL com `docker compose up -d postgres`.
2. Configure `ConnectionStrings:DefaultConnection` em `src/OrcaFacil.Web/appsettings.Development.json`.
3. Restaure e compile com `dotnet restore OrcaFacil.sln` e `dotnet build OrcaFacil.sln`.
4. Aplique migrations com `dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web`.
5. Execute com `dotnet run --project src/OrcaFacil.Web`.

### SuperAdmin opcional

Defina `ORCAFACIL_ADMIN_EMAIL` e `ORCAFACIL_ADMIN_PASSWORD` antes de iniciar a aplicação para habilitar seed operacional em ambientes que chamam o bootstrap administrativo.

### PDFs

Use `GET /api/documents/{id}/pdf` autenticado. Usuários Free recebem a marca “Gerado com OrçaFácil — MNSOFT”; usuários Pro geram PDF sem marca.

### Publicação IIS

Publique com `dotnet publish src/OrcaFacil.Web -c Release -o ./publish`, instale o Hosting Bundle do ASP.NET Core no servidor, configure o App Pool sem código gerenciado e aponte o site para a pasta publicada.

## MVP ASP.NET Core/PostgreSQL

### Rodar local

```bash
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln
dotnet test OrcaFacil.sln
dotnet run --project src/OrcaFacil.Web
```

### Banco e migrations

```bash
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

A migration inicial cria tabelas nos schemas `identity`, `core`, `billing`, `admin`, `logs` e `public_access`.

### SuperAdmin

Configure as chaves esperadas pelo `SuperAdminSeeder` em `appsettings`/variáveis de ambiente e inicie o Web para criar o usuário administrativo.

### Telas principais

- `/Auth/Register` e `/Auth/Login` para cadastro/login.
- `/Dashboard` para métricas.
- `/Profile` para emitente.
- `/Documents` para histórico.
- `/Documents/CreateBudget` e `/Documents/CreateReceipt` para novos documentos.
- `/p/{token}` para aprovação pública.
- `/Admin/Dashboard` para Admin Geral.

### PDF e IIS

Baixe PDFs autenticados por `/Documents/Pdf/{id}`. Para IIS, execute `publish-iis.bat` e siga `docs/DEPLOY-IIS-ASPNET.md`.
