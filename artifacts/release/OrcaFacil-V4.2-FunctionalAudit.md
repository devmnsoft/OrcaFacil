# OrçaFácil V4.2 — Auditoria funcional da Sprint 41

Data: 2026-08-27  
Escopo: exclusivamente a solução ASP.NET atual (`OrcaFacil.sln`, `src/OrcaFacil.*`, `database`, `scripts` e testes associados).

## Método

A auditoria combina varredura estática das camadas Web, Application, Domain, Persistence e Infrastructure com os validadores executáveis da Sprint 41. A varredura cobre implementações pendentes, links vazios, botões sem tipo, desativação de antiforgery, métricas aleatórias e entradas livres de identificadores técnicos. Os validadores especializados existentes continuam responsáveis pelos contratos funcionais de cada módulo.

## Resultado e classificação

| Prioridade | Achado | Tratamento |
| --- | --- | --- |
| P0 | Nenhum `NotImplementedException`, `href="#"`, `javascript:void` ou `Math.random` encontrado no escopo ASP.NET verificado. | Bloqueadores incorporados a `sprint41-check-core.mjs`. |
| P1 | O filtro de recibos solicitava que o operador digitasse `ClientId`. | Substituído por seletor abastecido por consulta real, limitada à conta corrente e apenas a clientes ativos/não excluídos. |
| P1 | Os nomes de checks requeridos pela Sprint 41 não estavam integralmente expostos no `package.json`. | Adicionados entry points para fluxo funcional, regras, comércio, portais, suporte, omnichannel, BI, workflows e segurança Git. |
| P2 | A inspeção de formulários precisava distinguir a proteção automática do Form Tag Helper da desativação explícita. | O check reprova `asp-antiforgery="false"`; formulários POST Razor permanecem cobertos pelo antiforgery automático do framework. |
| P3 | Checks de botões acessíveis e ordem de headings não tinham comandos dedicados. | Adicionados wrappers sobre o núcleo consolidado do design system. |

## Cobertura funcional observada

Os contratos automatizados existentes confirmam rotas e transições reais do fluxo comercial, projeção segura de propostas públicas, autenticação externa por token, isolamento de portais, Service Desk, conversas omnichannel, relatórios executivos e workflows configuráveis. O novo agregador não simula persistência nem substitui testes de integração: ele impede regressões estáticas antes que alcancem build, teste ou homologação.

## Pendências e limitações reais

- O SDK .NET não está instalado no ambiente desta execução; build, testes, publish e inicialização do site devem ser repetidos em agente/CI com .NET SDK compatível.
- `npm ci` foi bloqueado por HTTP 403 ao baixar `esbuild`; os checks Node sem dependências externas puderam ser executados com os artefatos versionados presentes.
- Sem aplicação executável neste ambiente, não foi possível realizar validação manual autenticada, inspeção do console ou captura responsiva no navegador.
- A disponibilidade e o schema do PostgreSQL devem ser confirmados no ambiente de homologação; nenhuma migration destrutiva foi criada nesta entrega.

## Critério para liberação

A entrega fica tecnicamente condicionada à execução verde dos builds Debug/Release, testes, publish, checks npm completos e roteiro manual em ambiente com SDK, banco e navegador. Os checks criados nesta sprint devem permanecer no pipeline para impedir recorrência dos bloqueadores encontrados.
