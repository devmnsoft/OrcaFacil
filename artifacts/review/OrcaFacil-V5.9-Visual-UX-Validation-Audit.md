# OrçaFácil V5.9 — auditoria visual, UX e validação

## Escopo e método

A auditoria cobre exclusivamente as Razor Pages do projeto ASP.NET atual listadas em `orcafacil-pages-inventory-v59.txt`. A revisão estática priorizou os bloqueadores P0/P1: feedback global, confirmação de ações críticas, prevenção de duplo envio, navegação, validação, acessibilidade de diálogos e comportamento mobile.

## Resultado implementado

- **P0:** os layouts autenticado e público usam um único host Razor para toast e confirmação, sem dependência de Bootstrap ou CDN.
- **P1:** formulários opt-in com `data-submit-lock` validam antes do bloqueio, expõem `aria-busy` e apresentam estado de processamento.
- **P1:** diálogos recebem inicialização idempotente e contenção de foco por teclado; o gerenciador já mantém fechamento por `Escape` e restauração de foco.
- **P1:** o feedback considera safe areas mobile, foco visível e movimento reduzido.
- **P2:** contratos automatizados V5.9 preservam os checks V5.8 e verificam os novos pontos de integração.

## Matriz das jornadas críticas

| Jornada | Prioridade | Evidência / decisão |
|---|---:|---|
| Home, login, registro e recuperação | P1 | Layout público, toast e formulários globais homologados |
| Onboarding e dashboard | P1 | Estruturas premium e dados reais preservados pelos checks existentes |
| Clientes e Cliente 360 | P1 | Empty states e padrões de formulário preservados |
| Documents/New e Details | P0/P1 | Wizard e validação existentes preservados; feedback global consolidado |
| CommercialRoutine | P0/P1 | POST antiforgery, validação, submit lock e confirmação preservados |
| OS, campo, financeiro e fiscal | P1 | Confirmações declarativas e feedback global disponíveis sem alterar regras |
| Projetos, CS, BI, automação e dados | P1/P2 | Checks de não-falsificação e componentes compartilhados preservados |
| Admin e System Health | P0/P1 | Checks de segurança/design preservados; nenhuma informação técnica adicionada |
| Portais cliente/parceiro | P0/P1 | Layouts e isolamento existentes preservados; nenhuma informação interna adicionada |

## Pendências P3

Microajustes visuais específicos dependem de homologação com dados representativos, perfis reais e captura multi-viewport em ambiente com SDK .NET e navegador. Eles não devem ser resolvidos com conteúdo fictício ou alteração de regra de negócio.
