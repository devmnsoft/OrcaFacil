# Pré-auditoria — OrçaFácil V5.3 / Sprint 52

Data: 2026-08-31
Escopo: exclusivamente a solução ASP.NET atual (`src`, `database`, `scripts` e `tests`).

## Resultado estrutural

- Não foi encontrado fallback operacional `Database=unavailable` ou `127.0.0.1:1`; as ocorrências existentes são validadores/testes que rejeitam essas sentinelas.
- O módulo anterior de importação já exigia upload, validação e confirmação, mas cobria somente clientes e serviços e não possuía contrato reutilizável de prévia/rollback.
- A tela anterior de qualidade usava consultas reais e isolamento por `AccountId`, mas não oferecia score explicável nem catálogo de regras versionável.
- As permissões existentes eram apenas `DataQuality.View` e `DataQuality.Manage`.

## Riscos tratados nesta entrega

1. Isolamento de conta no motor, detecção, commit e mesclagem.
2. Proibição de mesclagem entre contas e de exclusão física.
3. Prévia, confirmação, motivo e permissão forte antes de mesclar.
4. Prévia obrigatória, validação por linha e rollback bloqueado após alteração posterior.
5. Score determinístico ponderado por severidade, sem aleatoriedade.
6. Mascaramento de valores sensíveis e revisão humana para ações governadas.

## Limitação do ambiente de auditoria

O SDK `dotnet` não está instalado no contêiner. O `npm ci` também foi bloqueado com HTTP 403 pelo registry; portanto, builds, testes e execução no navegador precisam ser repetidos em um agente com .NET 10 e acesso ao registry.
