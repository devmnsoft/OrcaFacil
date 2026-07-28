# Estado do build

Atualizado em 27/07/2026 para **Release Candidate Operacional 1**, commit-base `767bb2c`.

## Ambiente

- SDK requerido pelos projetos: .NET 10 (`net10.0`).
- SDK disponível no container: nenhum; `dotnet --info` retorna `dotnet: command not found`.
- Versão efetivamente utilizada: **nenhuma**, pois o executável não existe na imagem.

## Execução inicial

| Comando | Resultado |
| --- | --- |
| `dotnet --info` | Bloqueado: executável ausente |
| `dotnet clean OrcaFacil.sln` | Não executável sem SDK |
| `dotnet restore OrcaFacil.sln` | Não executável sem SDK |
| `dotnet build OrcaFacil.sln` | Não executável sem SDK |
| `dotnet test OrcaFacil.sln` | Não executável sem SDK |

- Resultado do restore: **não executado** (`dotnet: command not found`, exit code 127).
- Resultado do build: **não executado** (`dotnet: command not found`, exit code 127).
- Quantidade de warnings do compilador: **indisponível**; não houve compilação.
- Resultado dos testes: **não executado** (`dotnet: command not found`, exit code 127).
- Testes ignorados: **indisponível**; o runner não iniciou.
- Limitação: imagem sem SDK .NET 10 e sem PowerShell para o validador Razor.

## Verificações estáticas desta etapa

| Comando | Resultado |
|---|---|
| `node scripts/check-ui-contrast.mjs` | Aprovado; 181 arquivos verificados |
| `node scripts/check-js.mjs` | Aprovado; 83 arquivos verificados |
| `git diff --check` | Aprovado |
| `powershell -ExecutionPolicy Bypass -File scripts/check-razor-directives.ps1` | Não executado; PowerShell ausente |

Não há declaração de build ou testes aprovados. As verificações estáticas de diff e os scripts Node são registradas no relatório final da alteração. O próximo ambiente de CI deve executar, nesta ordem:

```bash
dotnet --info
dotnet clean OrcaFacil.sln
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln --no-restore
dotnet test OrcaFacil.sln --no-build
```

Depois deve executar os validadores Razor, contraste e JavaScript e iniciar a aplicação contra PostgreSQL com migrations aplicadas.
