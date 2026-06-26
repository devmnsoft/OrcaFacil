# Consolidação PlantãoPro — execução bloqueada por repositório incorreto

Data da verificação: 2026-06-26.

## Diagnóstico real do workspace

A rodada solicitada para o projeto **PlantãoPro Saúde 360** não pôde ser executada neste workspace porque o diretório atual não contém o repositório ASP.NET Core/PostgreSQL/Dapper informado na demanda.

O workspace atual é o projeto **OrçaFácil**, com aplicação Node/Firebase/HTML estática. Foram encontrados arquivos como `package.json`, `server.js`, `firebase.json`, `functions/index.js` e páginas em `public/`. Não foram encontrados os caminhos esperados do PlantãoPro, como:

- `backend/PlantaoPro.Api/PlantaoPro.Api.csproj`;
- `backend/PlantaoPro.Web/PlantaoPro.Web.csproj`;
- `backend/PlantaoPro.Tests/`;
- `database/`;
- controllers ASP.NET Core MVC/API do PlantãoPro;
- views Razor do PlantãoPro.

Além disso, o repositório local não possui remote `origin` configurado e a branch atual é `work`, impossibilitando executar com segurança a sequência solicitada `git checkout main`, `git pull` e criação da branch `codex/consolidacao-funcional-forms-ux-premium` neste clone.

## Comandos executados

```bash
pwd
git branch --show-current
git remote -v
git status --short --untracked-files=no | head -20
find . -path './node_modules' -prune -o -path './functions/node_modules' -prune -o -maxdepth 2 -type f | sort | sed -n '1,80p'
dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj
dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

## Resultado dos builds solicitados

Os comandos `dotnet` não puderam ser executados porque o SDK/CLI `dotnet` não está disponível neste ambiente e os projetos `.csproj` solicitados não existem neste workspace.

Resultado observado:

```text
/bin/bash: dotnet: command not found
```

## Estado do Git antes da alteração deste relatório

O workspace já possuía deleções em `node_modules/` antes desta intervenção. Essas alterações não pertencem à rodada PlantãoPro e não foram adicionadas ao commit.

## Decisão técnica

Não foram implementadas alterações em código ASP.NET Core, Razor, API, PostgreSQL, Dapper, migrations ou seeds porque fazê-lo neste repositório criaria artefatos fora do projeto correto e aumentaria o risco de quebrar o produto atualmente presente no workspace.

## Pendência para retomada

Para executar a rodada completa, é necessário disponibilizar o clone correto do repositório PlantãoPro, com os diretórios esperados:

- `backend/PlantaoPro.Api/`;
- `backend/PlantaoPro.Web/`;
- `backend/PlantaoPro.Tests/`;
- `database/`;
- `docs/` do PlantãoPro.

Com o repositório correto, a execução deve ser retomada a partir da auditoria inicial, seguida da criação da matriz funcional, reorganização do menu, lookups, formulários específicos, UX premium, migrations/seeds, QA funcional, checklist LGPD e demo funcional.
