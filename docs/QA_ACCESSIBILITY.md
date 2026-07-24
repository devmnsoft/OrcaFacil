# QA Accessibility — OrçaFácil

## Auditoria automática de contraste

Execute:

```bash
node scripts/check-ui-contrast.mjs
```

O script verifica arquivos `.cshtml`, `.html`, `.css` e `.js` versionados e bloqueia padrões simples de regressão:

- `text-white` ou `color: #fff` em cards/fundos claros;
- `text-muted` em seções escuras sem override claro;
- combinações conhecidas de azul escuro sobre azul escuro;
- cards claros declarando texto branco.

## Checklist manual WCAG AA

- Navegar por teclado em landing, cadastro, login, dashboard, clientes, documentos, assinatura e admin.
- Confirmar foco visível em links, botões, inputs e menus offcanvas.
- Validar leitura por screen reader dos botões de ícone e ações críticas.
- Testar viewport de 320px sem overflow horizontal.
- Conferir contraste de textos secundários em cards, sidebar e hero.
- Garantir mensagens sem stack trace para usuário comum.

## Limitações

O script é preventivo e não substitui teste visual, axe/Lighthouse ou revisão com tecnologias assistivas.
