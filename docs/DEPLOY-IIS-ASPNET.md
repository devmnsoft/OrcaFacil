# Publicação IIS ASP.NET Core

1. Instale o .NET 10 SDK/Runtime e o ASP.NET Core Hosting Bundle no servidor.
2. Configure PostgreSQL e a connection string `DefaultConnection`.
3. Execute `dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web`.
4. Execute `publish-iis.bat`.
5. Aponte o site IIS para `publish/iis` e use Application Pool `No Managed Code`.

## Preparar PostgreSQL sem Docker

O IIS não usa Docker para hospedar a aplicação. O PostgreSQL pode estar no próprio servidor Windows, em outro servidor Windows/Linux ou em serviço remoto gerenciado.

1. Instale ou aponte para PostgreSQL 15+ ou 17.
2. Crie a database `orcafacil` e um usuário dedicado, por exemplo `orcafacil_user`.
3. Antes do deploy, execute o script completo:

```bash
psql -h <host-postgres> -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

4. Configure a connection string por variável de ambiente no servidor/IIS:

```powershell
[Environment]::SetEnvironmentVariable(
  "ConnectionStrings__DefaultConnection",
  "Host=<host-postgres>;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=<senha-segura>",
  "Machine")
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
```

5. Reinicie o Application Pool após alterar variáveis de ambiente.
6. Valide `https://seu-site/health`; o health check verifica conexão real com PostgreSQL e existência das tabelas base.

Não use a senha de desenvolvimento em produção. O arquivo `src/OrcaFacil.Web/appsettings.Production.json` deve permanecer sem segredo real, priorizando `ConnectionStrings__DefaultConnection`.

## PostgreSQL no IIS

Em produção no IIS, configure a connection string por variável de ambiente do Application Pool:

```text
ConnectionStrings__DefaultConnection=Host=SEU_HOST;Port=5432;Database=orcafacil;Username=SEU_USUARIO;Password=SUA_SENHA
```

Antes de publicar, execute `database/script_completop.sql` no PostgreSQL local/remoto. Após subir o site, valide `/health/db` e acesse `/Admin/Settings/Database` com usuário SuperAdmin para confirmar schema `orcafacil` e tabelas obrigatórias.
