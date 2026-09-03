# OrçaFácil V5.8 — auditoria de design, formulários e validação

## Escopo e método

Auditoria estática do projeto ASP.NET atual. O inventário adjacente registra todas as Razor Pages. A revisão combinou busca por formulários, controles, botões, links, headings, estados vazios, feedback, layouts e dependências JavaScript. Os checks V5.8 automatizam os bloqueadores objetivos e devem ser executados com `npm run check:ui-total-v58`.

## Implementação desta revisão

- Design system promovido para V5.8 com tokens de informação e controles, estados de validação, alertas, drawers, tabs, métricas, seções e ações de formulário.
- Host global autenticado passou a carregar overlays e feedback; confirmação acessível não recorre mais a `confirm()` nativo.
- `/Documents/New` substituiu links-fragmento usados como comandos por botões semânticos, navegação progressiva e foco previsível.
- `/CommercialRoutine` recebeu antiforgery explícito, resumo de validação, ajuda contextual, bloqueio de duplo envio e confirmação preservadora para conclusão.
- Catálogos centralizam mensagens humanas de validação e feedback sem expor exceções, segredos ou stack traces.

## Áreas revisadas

Home pública e autenticação; onboarding; dashboard; clientes e Cliente 360; documentos e novo orçamento; rotina comercial; OS, agenda e campo; ativos e manutenção; financeiro, pagamentos e fiscal; projetos, Customer Success e BI; automação e governança; Admin, usuários, permissões e System Health; portais do cliente e parceiro; erros, vazios e layouts móveis.

## Evidências e pendências

Os testes estruturais e scripts V5.8 cobrem o contrato compartilhado e jornadas críticas. Validação dinâmica, console e viewports dependem de runtime ASP.NET e navegador; devem ser repetidos no pipeline que disponha do SDK .NET e browser.
