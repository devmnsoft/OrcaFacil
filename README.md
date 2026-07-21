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

A migration inicial e o script completo criam tabelas no schema único `orcafacil`.

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

## Rodar sem Docker

O OrçaFácil não depende exclusivamente do Docker nem exclusivamente de migrations. Use PostgreSQL local, remoto Windows/Linux ou Docker.

1. Instale PostgreSQL 15+ ou 17.
2. Crie a database e o usuário:

```sql
CREATE DATABASE orcafacil;
CREATE USER orcafacil_user WITH PASSWORD '123456';
GRANT ALL PRIVILEGES ON DATABASE orcafacil TO orcafacil_user;
```

3. Execute o script completo:

```bash
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

4. Configure `ConnectionStrings:DefaultConnection` em `src/OrcaFacil.Web/appsettings.Development.json`, user-secrets ou variável de ambiente:

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=123456"
```

5. Restaure, compile e execute:

```bash
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln
dotnet run --project src/OrcaFacil.Web
```

### Banco: migrations ou script manual

Opção A — migrations:

```bash
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

Opção B — script SQL manual:

```bash
psql -h localhost -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

### Docker com script completo

O `docker-compose.yml` usa `database/script_completop.sql` como script principal de inicialização do PostgreSQL. Para banco limpo com Docker:

```bash
docker compose up -d postgres
```

Se o volume `orcafacil_pgdata` já existir, o PostgreSQL não reexecuta scripts de `/docker-entrypoint-initdb.d`; execute o script manualmente ou recrie o volume em desenvolvimento.

## Banco de Dados PostgreSQL

Todas as tabelas do OrçaFácil ficam no schema PostgreSQL `orcafacil`.

Exemplos:

- `orcafacil.users`
- `orcafacil.documents`
- `orcafacil.document_items`
- `orcafacil.public_quotes`

O script completo e idempotente do banco está em `database/script_completop.sql` e é o mesmo usado pelo Docker Compose.

## Rodar sem Docker

1. Instale o PostgreSQL.
2. Crie o banco `orcafacil`.
3. Crie o usuário `orcafacil_user`.
4. Execute:

   ```bash
   psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
   ```

5. Configure a connection string em `src/OrcaFacil.Web/appsettings.Development.json`, user-secrets ou na variável de ambiente `ConnectionStrings__DefaultConnection`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=123456"
     }
   }
   ```

6. Rode a aplicação:

   ```bash
   dotnet run --project src/OrcaFacil.Web
   ```

## Rodar com Docker

Configure `ORCAFACIL_DB_PASSWORD` em `.env` e execute:

```bash
docker compose up -d postgres
```

O container inicializa bancos novos usando `database/script_completop.sql`.

## PostgreSQL padronizado (schema único `orcafacil`)

O MVP ASP.NET Core usa PostgreSQL com todas as tabelas da aplicação no schema único `orcafacil`. Não crie nem utilize os schemas antigos `identity`, `core`, `billing`, `admin`, `logs` ou `public_access`.

### Rodar sem Docker

1. Instale PostgreSQL localmente.
2. Crie o banco `orcafacil` e o usuário `orcafacil_user`.
3. Configure `src/OrcaFacil.Web/appsettings.Development.json` ou a variável `ConnectionStrings__DefaultConnection`.
4. Execute o SQL idempotente:

```bash
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

### Rodar com Docker

O `docker-compose.yml` monta `./database/script_completop.sql` em `/docker-entrypoint-initdb.d/01-script-completo.sql`. O entrypoint oficial do PostgreSQL executa scripts dessa pasta somente quando o volume de dados é novo; se o volume já existir, recrie o volume ou execute o script manualmente com `psql`.

```bash
docker compose up -d
```

### Migrations e diagnóstico

Para validar o app:

```bash
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln
dotnet test OrcaFacil.sln
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

Endpoints úteis:

- `/health`
- `/health/db`
- `/health/version`

Administradores SuperAdmin podem acessar **Admin > Settings > Database** em `/Admin/Settings/Database` para ver conexão, schema, tabelas encontradas/ausentes e instruções de execução do SQL sem exposição de senha.

## Evolução UX/UI premium

O OrçaFácil possui um design system em `src/OrcaFacil.Web/wwwroot/css/app.css`, layout autenticado com sidebar/offcanvas, layout público comercial, landing page SaaS, telas modernas de login/cadastro, dashboard com métricas reais, histórico responsivo, formulários guiados para orçamento/recibo, perfil do emitente, assinatura, suporte, aprovação pública e Admin Geral com visual profissional.

Para validar a experiência visual, consulte `docs/UX-UI.md` e execute o checklist em `docs/QA_UI.md`.
