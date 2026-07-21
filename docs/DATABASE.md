# Banco de Dados — OrçaFácil

O OrçaFácil usa PostgreSQL 15+ ou 17 e pode ser inicializado por migrations EF Core ou pelo script SQL manual `database/script_completop.sql`.

## Schemas

- `identity`: usuários e autenticação.
- `core`: perfil do emitente, documentos, itens e uso mensal.
- `billing`: assinaturas e pagamentos manuais/futuros provedores.
- `admin`: configurações administrativas e notificações.
- `logs`: auditoria, logs de sistema e erros.
- `public_access`: links públicos de aprovação de orçamento.

## Tabelas principais

- `identity.users`: cadastro, papel (`User`, `Admin`, `SuperAdmin`), plano (`Free`, `Pro`) e flags de bloqueio.
- `core.issuer_profiles`: dados do emitente vinculados de forma única ao usuário.
- `core.documents`: orçamentos/recibos, totais, token público, decisão do cliente e exclusão lógica.
- `core.document_items`: itens do documento com quantidade, preço, desconto e total.
- `public_access.public_quotes`: controle de acesso/decisão de orçamento público.
- `core.user_usage`: contadores mensais de limites Free/Pro.
- `billing.subscriptions` e `billing.payments`: base de cobrança manual.
- `admin.admin_settings`: seeds mínimos `company`, `contact`, `plans`, `logging`, `security`, `chatbot`, `telegram`, `theme` e `terms`.
- `logs.audit_logs`, `logs.system_logs`, `logs.system_errors`: auditoria, diagnóstico e falhas.

## Executar o script completo

Antes de executar, crie a database e o usuário:

```sql
CREATE DATABASE orcafacil;
CREATE USER orcafacil_user WITH PASSWORD '123456';
GRANT ALL PRIVILEGES ON DATABASE orcafacil TO orcafacil_user;
```

Execute:

```bash
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

No Windows, também é possível usar:

```powershell
cd database
.\executar_script_postgres.ps1 -HostName localhost -Port 5432 -Database orcafacil -User orcafacil_user
```

ou `database\executar_script_postgres.bat`. A senha é solicitada pelo `psql`.

## Rodar sem Docker

1. Instale PostgreSQL 15+ ou 17 localmente ou use um servidor remoto.
2. Crie `orcafacil` e `orcafacil_user`.
3. Execute `database/script_completop.sql`.
4. Configure `ConnectionStrings:DefaultConnection` em `src/OrcaFacil.Web/appsettings.Development.json`, user-secrets ou variável `ConnectionStrings__DefaultConnection`.
5. Rode `dotnet restore OrcaFacil.sln`, `dotnet build OrcaFacil.sln` e `dotnet run --project src/OrcaFacil.Web`.

## Rodar com Docker

O `docker-compose.yml` monta `database/script_completop.sql` em `/docker-entrypoint-initdb.d/01-script-completo.sql`. O entrypoint do PostgreSQL executa o script na primeira criação do volume.

```bash
docker compose up -d postgres
```

Se o volume já existia antes da inclusão do script, recrie o banco de desenvolvimento ou execute o script manualmente dentro/fora do container.

## Migrations EF Core

Opção A — migrations:

```bash
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

Opção B — SQL manual:

```bash
psql -h localhost -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

O script manual existe para bootstrap confiável fora do Docker e fora do fluxo de migrations. As migrations continuam úteis para evolução incremental do modelo.

## Resetar banco em desenvolvimento

```bash
dropdb -h localhost -U postgres orcafacil
createdb -h localhost -U postgres -O orcafacil_user orcafacil
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

Em Docker, remova o volume somente em desenvolvimento:

```bash
docker compose down -v
docker compose up -d postgres
```

## SuperAdmin

Não existe senha fixa no SQL. Para criar o SuperAdmin operacional, configure as variáveis antes de iniciar a aplicação:

```bash
ORCAFACIL_ADMIN_EMAIL=admin@seudominio.com
ORCAFACIL_ADMIN_PASSWORD=troque-esta-senha
```

Exemplo SQL manual apenas para laboratório e nunca ativo por padrão:

```sql
-- INSERT INTO identity.users (name, email, password_hash, role, plan)
-- VALUES ('SuperAdmin', 'admin@example.com', '<hash-gerado-pela-aplicacao>', 'SuperAdmin', 'Pro');
```
