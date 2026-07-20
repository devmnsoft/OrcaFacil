# OrçaFácil

SaaS freemium da **MNSOFT** (CNPJ 18.160.057/0001-13) para autônomos, MEIs e pequenos prestadores criarem orçamentos, recibos, PDFs backend, histórico e links públicos de aprovação.

## Stack
ASP.NET Core 10, C# preview, PostgreSQL, EF Core, Dapper, Clean Architecture, DDD, CQRS simples, FluentValidation, Serilog, QuestPDF, Razor Pages/MVC, Bootstrap 5 e Health Checks.

## Arquitetura
- `src/OrcaFacil.Domain`: entidades, enums, value objects e regras.
- `src/OrcaFacil.Application`: comandos, queries, DTOs, casos de uso e limites de plano.
- `src/OrcaFacil.Persistence`: DbContext, mapeamentos EF, repositórios e Dapper.
- `src/OrcaFacil.Infrastructure`: PDF, hash de senha, middlewares e integrações futuras.
- `src/OrcaFacil.Api`: endpoints REST.
- `src/OrcaFacil.Web`: Razor Pages Bootstrap 5.

## Docker
```bash
cp .env.example .env
docker compose up -d postgres
```

## Local
```bash
dotnet restore OrcaFacil.sln
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
dotnet run --project src/OrcaFacil.Web
```

## SuperAdmin
Defina `ORCAFACIL_ADMIN_EMAIL` e `ORCAFACIL_ADMIN_PASSWORD`. Sem essas variáveis nenhum admin inicial é criado.

## Testes
```bash
dotnet test OrcaFacil.sln
```

## IIS
Consulte `docs/DEPLOY-IIS-ASPNET.md` ou execute `publish-iis.bat`.

## Roadmap
Mercado Pago real, nota fiscal, WhatsApp API oficial, assinatura digital ICP-Brasil, BI e IA backend segura estão preparados para evolução futura, mas não implementados nesta primeira reescrita.
