# Estado do build

Atualizado em 27/07/2026 para o branch de trabalho baseado no PR 56.

## Ambiente

- SDK requerido pelos projetos: .NET 10 (`net10.0`).
- SDK disponível no container: nenhum; `dotnet --info` retorna `dotnet: command not found`.
- Tentativa de instalação: download de `https://dot.net/v1/dotnet-install.sh` bloqueado pelo proxy com HTTP 403.

## Execução inicial

| Comando | Resultado |
| --- | --- |
| `dotnet --info` | Bloqueado: executável ausente |
| `dotnet clean OrcaFacil.sln` | Não executável sem SDK |
| `dotnet restore OrcaFacil.sln` | Não executável sem SDK |
| `dotnet build OrcaFacil.sln` | Não executável sem SDK |
| `dotnet test OrcaFacil.sln` | Não executável sem SDK |

Não há declaração de build ou testes aprovados. As verificações estáticas de diff e os scripts Node são registradas no relatório final da alteração. O próximo ambiente de CI deve executar, nesta ordem:

```bash
dotnet --info
dotnet clean OrcaFacil.sln
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln --no-restore
dotnet test OrcaFacil.sln --no-build
```

Depois deve executar os validadores Razor, contraste e JavaScript e iniciar a aplicação contra PostgreSQL com migrations aplicadas.
