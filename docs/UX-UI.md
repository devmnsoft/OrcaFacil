# UX/UI do OrçaFácil

## Identidade visual
O OrçaFácil usa uma identidade premium, simples e confiável para autônomos, MEIs e pequenos prestadores de serviço. A paleta combina azul-marinho, azul-vivo, fundos suaves, cards brancos, bordas arredondadas e sombras leves.

## Marca MNSOFT
A assinatura institucional é **OrçaFácil — um produto MNSOFT**. A logo deve ficar em `src/OrcaFacil.Web/wwwroot/img/mnsoft-logo.png`. Quando a imagem não existir, as telas usam fallback textual “MNSOFT” e não quebram o layout.

## Tom de voz
Use linguagem simples: “seus dados”, “seu cliente”, “gerar PDF”, “enviar ao cliente” e “histórico”. Evite termos técnicos como endpoint, schema, metadados ou payload nas telas para usuários finais.

## Jornada do primeiro uso
1. Usuário entende a landing.
2. Clica em Começar grátis.
3. Cria conta.
4. Entra no onboarding.
5. Cadastra os dados do emitente.
6. Cria o primeiro orçamento.
7. Gera PDF pelo histórico ou detalhes.
8. Conhece o Pro para remover a marca do PDF.

## Componentes
- Page header: título, microtexto e ação principal.
- Metric card: número grande, rótulo e contexto.
- Empty state: ícone, título claro, texto curto e CTA.
- Explainer card: explica a finalidade da tela.
- Step card: mostra progresso e próximos passos.
- Plan badge e status badge: indicar Free/Pro e status sem depender só de cor.

## Responsividade e acessibilidade
O menu lateral vira offcanvas no celular; cards e botões ganham espaçamento; tabelas têm alternativa em cards ou scroll. Inputs precisam de labels, foco visível, contraste adequado e mensagens claras.

## Padrões de botões
- Primário: ação principal da tela.
- Sucesso: criação, aprovação ou ativação comercial.
- Outline: ações secundárias.
- Danger: exclusões ou recusas, sempre com confirmação.

## Status
Use badges com texto explícito: Rascunho, Emitido, Aprovado, Recusado, Cancelado, Free e Pro. Não dependa apenas da cor.
