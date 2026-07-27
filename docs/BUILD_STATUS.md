# Build Status — auditoria RC

Data da última execução: 2026-07-27 (UTC).

## Baseline obrigatório

| Comando | Resultado | Observações |
|---|---|---|
| `dotnet --info` | Bloqueado | O executável `dotnet` não está instalado no ambiente (`/bin/bash: dotnet: command not found`). |
| `dotnet clean OrcaFacil.sln` | Bloqueado | Falhou com código 127 porque o SDK .NET não está disponível. |
| `dotnet restore OrcaFacil.sln` | Bloqueado | Falhou com código 127 porque o SDK .NET não está disponível. |
| `dotnet build OrcaFacil.sln` | Bloqueado | Falhou com código 127 porque o SDK .NET não está disponível. |
| `dotnet test OrcaFacil.sln` | Bloqueado | Falhou com código 127 porque o SDK .NET não está disponível. |

## SDK encontrado

- SDK encontrado: **não**.
- Versão do SDK: **não identificável neste ambiente**.
- Limitação do ambiente: a imagem de execução atual não possui o CLI `dotnet`; portanto restore, build, testes .NET, compilação Razor e validação de migrations não puderam ser executados localmente.

## Projetos identificados na solução/repositório

- `src/OrcaFacil.Domain/OrcaFacil.Domain.csproj`
- `src/OrcaFacil.Application/OrcaFacil.Application.csproj`
- `src/OrcaFacil.Persistence/OrcaFacil.Persistence.csproj`
- `src/OrcaFacil.Infrastructure/OrcaFacil.Infrastructure.csproj`
- `src/OrcaFacil.Web/OrcaFacil.Web.csproj`
- `src/OrcaFacil.Api/OrcaFacil.Api.csproj`
- `tests/OrcaFacil.UnitTests/OrcaFacil.UnitTests.csproj`

## Warnings e erros

- Warnings: não validados por ausência do SDK .NET.
- Erros: os comandos .NET falharam por `dotnet: command not found`, não por erro compilado do código.
- Projetos compilados: nenhum neste ambiente.
- Projetos não compilados: todos os projetos .NET listados acima.

## Correções realizadas nesta etapa

- O acesso aos benefícios deixou de consultar `PlanCatalogDefinitions` em execução e passou a usar uma fonte EF baseada em `PlanVersion`, `Feature`, `PlanFeatureValue`, `Subscription` e `PlanOverride`.
- `GetUsageAsync` passou a medir dados por `AccountId` para clientes, documentos, PDFs, membros, aprovações públicas e modelos. Serviços são medidos pelos eventos reais de ativação existentes até a consolidação da entidade de catálogo de serviços.
- Overrides agora resolvem a `PlanVersion` e o `Plan` reais; a expiração volta à assinatura ou ao Grátis sem apagar dados.
- Foram adicionados vínculo `Document.ClientId`, referência de fatura no pagamento e colunas aditivas de isolamento para utilização e links públicos.
- Cookies passaram a carregar e validar `SessionVersion`; usuário bloqueado, usuário desativado, membro sem acesso ou conta bloqueada têm o cookie rejeitado.
- A migration e o script SQL foram alinhados de forma aditiva, mantendo os campos legados.

## Validações auxiliares de 2026-07-27

- `node scripts/check-ui-contrast.mjs`: passou; 181 arquivos verificados e nenhum padrão bloqueador.
- `git diff --check`: passou sem erro de whitespace.
- `powershell -ExecutionPolicy Bypass -File scripts/check-razor-directives.ps1`: bloqueado; `powershell` e `pwsh` não estão instalados.
- Tentativa de obter o instalador oficial do SDK com `curl https://dot.net/v1/dotnet-install.sh`: bloqueada pelo ambiente remoto com HTTP 403.

## Próxima validação necessária

Executar em uma máquina com .NET SDK compatível com o `TargetFramework` dos `.csproj`:

```bash
dotnet --info
dotnet clean OrcaFacil.sln
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln
dotnet test OrcaFacil.sln
node scripts/check-ui-contrast.mjs
```
