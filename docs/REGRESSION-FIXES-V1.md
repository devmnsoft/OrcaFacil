# Regressões — Release V1

**Versão:** 1.0.0 — **Data:** 19/08/2026

## Critério

- **P0:** impede build, publish, banco, autenticação, segurança ou fluxo essencial.
- **P1:** quebra fluxo principal, navegação, persistência, idempotência ou tela crítica.
- **P2:** prejudica usabilidade sem impedir o fluxo.
- **P3:** melhoria não bloqueante.

## Encontradas e corrigidas

| Prioridade | Regressão | Correção | Estado |
|---|---|---|---|
| P1 | Filtro do pipeline continha `form` sem método explícito, reprovando o contrato Razor e permitindo submissão ambígua. | Declarado `method="get"`, preservando o filtro progressivo em JavaScript e um fallback seguro. | Corrigida |
| P0 | O comando obrigatório `npm run check:release-final` não existia. | Criado gate agregado que exige artefatos operacionais e executa checks de escopo, banco, Razor, fluxos, segurança, backup e UI. | Corrigida |
| P1 | O check de schema cobria 21 tabelas, mas não verificava os conceitos finais de recebíveis, contratos/cobranças, ações e templates comerciais, nem suporte. | Gate ampliado para os 27 nomes físicos usados pelos mapeamentos EF. | Corrigida |
| P2 | Scripts de restore exigiam confirmação, mas não recomendavam explicitamente um backup imediatamente antes da operação. | Aviso obrigatório adicionado aos dois entrypoints de restore. | Corrigida |

## Pendências

Nenhum P0/P1 de código conhecido após os gates estáticos. A aprovação continua **bloqueada no ambiente desta execução** enquanto build/test/publish, PostgreSQL real e navegador não forem ensaiados, pois o host não dispõe do SDK .NET nem das ferramentas PostgreSQL. Isso é limitação de homologação, não resultado aprovado.
