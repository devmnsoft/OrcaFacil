# Banco de Dados PostgreSQL

O OrçaFácil usa um schema PostgreSQL único chamado `orcafacil`. No PostgreSQL, `orcafacil.users` significa `schema.tabela`; por isso todas as tabelas da aplicação ficam claramente agrupadas nesse namespace.

## Tabelas principais

- `orcafacil.users`
- `orcafacil.issuer_profiles`
- `orcafacil.documents`
- `orcafacil.document_items`
- `orcafacil.public_quotes`
- `orcafacil.user_usage`
- `orcafacil.subscriptions`
- `orcafacil.payments`
- `orcafacil.admin_settings`
- `orcafacil.notifications`
- `orcafacil.audit_logs`
- `orcafacil.system_logs`
- `orcafacil.system_errors`

Schemas antigos (`identity`, `core`, `billing`, `admin`, `logs`, `public_access`) não devem ser usados por scripts, EF Core ou Dapper.

## Script completo

O script idempotente fica em `database/script_completop.sql` e cria extensões, schema, tabelas, constraints, índices e seeds de `admin_settings`.

```bash
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

No Windows também existem helpers:

```bat
database\executar_script_postgres.bat
```

```powershell
.\database\executar_script_postgres.ps1 -Host localhost -Port 5432 -Database orcafacil -User orcafacil_user
```

## Rodar sem Docker

1. Instale o PostgreSQL.
2. Crie o banco `orcafacil`.
3. Crie o usuário `orcafacil_user` e conceda permissão no banco.
4. Execute `database/script_completop.sql`.
5. Configure `ConnectionStrings:DefaultConnection` em `src/OrcaFacil.Web/appsettings.Development.json`, user-secrets ou variável `ConnectionStrings__DefaultConnection`.
6. Execute `dotnet run --project src/OrcaFacil.Web`.

## Rodar com Docker

O `docker-compose.yml` monta `./database/script_completop.sql` em `/docker-entrypoint-initdb.d/01-script-completo.sql`. Em bancos novos, o PostgreSQL executa esse script automaticamente.

```bash
docker compose up -d postgres
```

## Migrations

Para criar uma migration a partir do modelo EF Core:

```bash
dotnet ef migrations add UseOrcaFacilSingleSchema --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

Para aplicar migrations:

```bash
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

## Reset em desenvolvimento

```sql
DROP SCHEMA IF EXISTS orcafacil CASCADE;
```

Depois execute novamente `database/script_completop.sql` ou as migrations.

## SuperAdmin

Não há senha fixa no SQL. A aplicação cria o SuperAdmin a partir das variáveis:

- `ORCAFACIL_ADMIN_EMAIL`
- `ORCAFACIL_ADMIN_PASSWORD`

## Validação

```sql
select schema_name from information_schema.schemata where schema_name = 'orcafacil';
select table_schema, table_name from information_schema.tables where table_schema = 'orcafacil' order by table_name;
select to_regclass('orcafacil.users'), to_regclass('orcafacil.documents'), to_regclass('orcafacil.system_errors');
```

A aplicação expõe `/health/db`, `/diagnostico` e `/Admin/Settings/Database` para validar conexão, schema e tabelas principais.
