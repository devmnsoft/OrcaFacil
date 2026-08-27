# OrçaFácil V4.1 — Correções visuais

## Entregue

- Fundação premium consolidada em módulos locais, sem CDN ou framework visual.
- Hierarquia consistente para cabeçalhos de página, superfícies, KPIs, filtros e ações.
- Tabelas com contêiner responsivo, cabeçalho fixo, foco e hover sem depender somente de cor.
- Navegação interna com agrupamento, estado ativo, áreas de toque e adaptação mobile.
- Timelines e kanbans com semântica visual compartilhada e fallback de rolagem.
- Empty, loading e skeleton states com suporte a movimento reduzido.
- Modais e formulários limitados à viewport; ações empilham em telas estreitas.
- Foco visível e modo de alto contraste reforçados.

## P0 corrigido pela fundação

Home, Login, Dashboard, Offline e o portal público de propostas recebem tokens, superfícies, controles e regras responsivas compartilhadas. Não foi localizada uma Razor Page própria de Portal do Parceiro no ASP.NET atual; nenhuma tela artificial foi criada para encobrir essa lacuna. O login preserva antiforgery, validação server-side e loading real do submit.

## P1 corrigido pela fundação

Admin, Clientes, Serviços, Orçamentos, OS, Financeiro, Pagamentos, Contratos, BI, Marketplace, Suporte, Omnichannel e Configurações recebem padrões compartilhados de tabelas, formulários, cards, navegação e feedback.

## Pendências de validação visual

A aprovação final em 320, 360, 390, 430, 768, 1024, 1366, 1440 e 1920 px depende de execução do ASP.NET com PostgreSQL e contas reais de cada perfil. Não foram inventados dados ou contornadas permissões para produzir capturas.
