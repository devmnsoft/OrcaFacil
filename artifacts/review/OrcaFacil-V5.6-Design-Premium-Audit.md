# OrçaFácil V5.6 — Auditoria de Design Premium

**Escopo:** somente a solução ASP.NET atual (`OrcaFacil.sln`, `src/OrcaFacil.*`, `tests`, `scripts` e este artefato). Nenhum frontend legado ou Firebase foi usado como fonte de interface.
**Data:** 02/09/2026
**Método:** inspeção estática de Razor/CSS/JavaScript, contratos automatizados da Sprint 55 e matriz responsiva. A inspeção dinâmica ficou condicionada à disponibilidade do .NET SDK, registrada em pendências.

## Critérios usados

Cada superfície foi avaliada em: visual, espaçamento, hierarquia, contraste, ícones, responsividade, UX, links/ações, empty state e loading. Prioridades: **P0** impede uso ou segurança; **P1** afeta tarefa principal; **P2** reduz consistência; **P3** é melhoria cosmética.

## Inventário e resultado

| Tela/superfície | Achados de visual, hierarquia, contraste, ícones e espaçamento | Responsividade e UX | Links, ações, empty/loading | Prioridade | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|
| Login | Card já era dividido, mas a folha `auth.css` não era carregada pela página e o estado de envio substituía todo o conteúdo sem anúncio dedicado. | Bom grid; faltava refinamento compacto e contexto de segurança. | POST/antiforgery preservados; suporte real. | P1 | CSS dedicado, fundo em camadas, confiança, `aria-live`, `aria-busy` e loading legível. | Validar visualmente em navegador real. |
| Home pública | Hero, prova do produto e CTAs têm hierarquia forte e sem números/depoimentos inventados. | Seções refluem; CTA e formulário têm estados reais. | Rotas por tag helper; empty state não se aplica à landing. | P2 | Componentes/tokens V5.6 passam a sustentar a superfície pública. | Teste visual dinâmico. |
| Dashboard | Dados reais, prioridade e KPIs têm origem no model; densidade entre seções ainda varia. | Cards e colunas possuem quebras para mobile. | Atalhos têm rotas; documentos possuem empty state. | P1 | Contratos automatizados protegem métricas reais, recomendação, atalhos e empty state. | Avaliar dados extremos em 320px. |
| Menu interno | Agrupamento, item ativo e contraste já existem; sidebar mantém linguagem sóbria. | Drawer móvel tem abrir, fechar, Escape e clique externo. | Sem `href="#"`/`javascript:void`; permissões continuam no partial/model. | P1 | Check V5.6 agregado ao validador existente. | Navegação autenticada requer sessão para inspeção manual. |
| Topbar | Barra móvel é concisa e mantém marca/contexto. | Alvos de 44px; safe-area preservada. | Ações reais de menu e conta. | P2 | Matriz responsiva formalizada. | Revisar nomes longos de usuário. |
| Breadcrumbs | Primitivo existia de forma dispersa. | Wrap é necessário em caminhos extensos. | Não cria rota artificial. | P2 | Contrato `.breadcrumb/.of-breadcrumb` consolidado. | Migrar páginas antigas progressivamente. |
| Clientes | Listagem e ações são funcionais; variação histórica de cards/tabelas. | Wrapper de tabela previne overflow da página. | Empty state e criação orientam próxima ação. | P1 | Primitivos de filtro, tabela responsiva e action card consolidados. | Browser com massa de dados extensa. |
| Cliente 360 | Contexto comercial é preservado em uma página, com timeline real. | Colunas devem empilhar no mobile. | Ações dependem de registro real; não solicita ID técnico. | P1 | Timeline, badges e summary cards padronizados. | Revisar anexos longos. |
| Orçamentos/Propostas | Listas têm status e ações; estilos antigos variam por módulo. | Tabela tem scroll contido e foco por linha. | Empty state aponta criação real. | P1 | `premium-table` e `responsive-table-card` adicionados sem remover classes. | Conversão gradual das tabelas legadas. |
| Novo orçamento/documento | Fluxo guiado existente, sem ClientId manual. | Grid responsivo e controles de 44px. | Loading e validação preservados. | P1 | Wizard, form panel e validation message formalizados. | Testar teclado e dados extensos no browser. |
| Rotina Comercial | Hero, resumo, filtros, timeline e action bar já têm contratos reais. | CSS contém breakpoint; ações continuam navegáveis. | Empty states dependem dos dados reais. | P1 | Check comercial existente foi ampliado pelo gate V5.6. | Sessão e banco necessários para revisão dinâmica. |
| OS, Agenda e Campo | Superfícies funcionais já possuem CSS modular; inconsistências menores de densidade. | Há contratos mobile específicos. | Nenhum botão fake introduzido. | P2 | Tokens, action bars, status e cards tornam-se base comum. | Migração visual por módulo em sprint posterior. |
| Financeiro e Fiscal | Conteúdo sensível e indicadores vêm dos serviços; tabelas são densas. | Scroll fica no wrapper, não na viewport. | Ações continuam protegidas por permissão. | P1 | Tabela, badges de risco e feedback reutilizáveis consolidados. | Validar colunas com moeda longa. |
| Projetos e Customer Success | Cards e kanban têm dados reais, mas estilos variam. | Kanban horizontal deve manter snap/scroll local. | Empty states permanecem orientados à ação. | P2 | Kanban card, health card e risk badge formalizados. | Inspeção dinâmica por permissão. |
| BI Executivo | KPIs não usam aleatoriedade; hierarquia depende da quantidade disponível. | Grid adaptativo. | Sem gráfico/card inventado nesta sprint. | P1 | Gate proíbe aleatoriedade no dashboard crítico. | Dataset amplo para validação visual. |
| Automação e Governança de Dados | Linguagem técnica é necessária, mas deve ser contextualizada. | Formulários e tabelas usam bases compartilhadas. | Ações críticas continuam reais. | P2 | Primitivos command/health/alert padronizados. | Revisar textos específicos com produto. |
| Admin | Métricas, contas e pagamentos são reais; tabelas estavam visualmente densas. | Layout modular; demanda teste em tablet. | Recalcular é POST real e tipado. | P1 | Gate valida H1, métricas, saúde e ausência de segredos. | Browser autenticado como SuperAdmin. |
| System Health | Saúde de banco/e-mail/readiness aparece sanitizada no Admin. | Diagnóstico cabe em cards empilháveis. | Link de readiness é endpoint real. | P0 | Teste impede `ConnectionString`/`StackTrace` na view. | Disponibilidade externa não pode ser simulada. |
| Portal do Cliente | Layout externo é separado; precisava de identidade visual dedicada. | Ações grandes e documento precisam empilhar. | Isolamento não foi alterado; sem custo, margem ou DRE no layout. | P1 | `portals.css` dedicado carregado pelo `_ClientLayout`. | Validar tokens reais e documento longo. |
| Portal do Parceiro | Compartilha requisitos de confiança e isolamento. | Base V5.6 é mobile-first. | Nenhuma visibilidade adicional foi criada. | P1 | Primitivos `.of-partner-portal`, ações e documentos. | Confirmar rotas disponíveis no ambiente integrado. |
| Telas de erro | Devem preservar correlação sem stack trace para usuário. | Texto e CTA precisam caber em 320px. | Próxima ação deve ser real. | P1 | Tokens de alerta/feedback permanecem sem mascarar falha. | Exercitar códigos 403/404/500 no browser. |
| Mobile (matriz) | Alvos, grids, drawers e cards são consistentes; risco principal são conteúdos extremos. | 320, 360, 390, 430, 768, 1024, 1366, 1440 e 1920 documentados no CSS/check. | Menu mantém foco/Escape; CTAs ocupam largura em telas estreitas. | P0 | Regras compactas, safe-area, empilhamento e gate de viewports. | Screenshot/browser bloqueado sem .NET SDK. |

## Componentes consolidados

`page-shell`, `page-hero`, `page-header`, `section-header`, `metric-card`, `summary-card`, `action-card`, `status-badge`, `priority-badge`, `risk-badge`, `empty-state`, `loading-state`, `filter-bar`, `action-bar`, `premium-table`, `responsive-table-card`, `form-panel`, `form-grid`, `input-group`, `validation-message`, toast/alert existentes, modal/drawer existentes, `timeline`, `kanban-card`, `wizard-steps`, `breadcrumb`, `quick-actions`, `command-card` e `health-card`.

## Pendências verificáveis

1. O contêiner não disponibiliza `dotnet`; build, testes Razor, execução de rotas e navegador autenticado não puderam ser realizados localmente.
2. A validação dinâmica deve cobrir dados longos, estados de erro reais, perfis de permissão e isolamento dos portais.
3. A migração das classes legadas para os novos aliases deve ser incremental para não quebrar Razor Pages.
4. As deleções preexistentes em `node_modules` foram preservadas e não fazem parte da Sprint 55.
