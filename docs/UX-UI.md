# UX/UI OrçaFácil

- Cadastro e login usam layout premium em duas colunas, card branco de alta legibilidade, área comercial azul escura e SVG textual.
- Textos em fundo claro usam `#1C2430` e textos secundários usam `#475569`.
- A marca MNSOFT possui fallback textual; a logo binária real deve ser adicionada manualmente fora deste PR.
- O rodapé público é único no layout compartilhado.

## Contraste, cadastro e senha

Use `.of-section-light` para fundos claros com texto escuro e `.of-section-dark` para fundos azul-escuro com texto branco. Cards e formulários em fundo claro devem usar `--of-text`, `--of-heading` e `--of-muted`.

A tela de cadastro usa duas colunas no desktop: painel comercial escuro com ilustração contextual e card branco com formulário. No mobile, o formulário aparece primeiro e os benefícios abaixo.

Campos de senha usam `.of-password-field`, cadeado único à esquerda e botão `data-password-toggle` com ícone de olho à direita. O tipo inicial deve ser sempre `password`.

## Evolução SaaS premium: modelos por profissão e orçamento guiado

- Contraste: páginas devem usar `.of-section-light` para fundo claro com texto escuro e `.of-section-dark` para fundo escuro com texto branco/alta opacidade.
- Rodapé premium: layout público em três colunas no desktop, empilhado no mobile, com marca OrçaFácil, links institucionais e dados MNSOFT sem overflow.
- Link `/#recursos`: a seção de recursos da landing possui `id="recursos"`; validar clique no menu público e scroll suave.
- Modelos por profissão: eletricista, pintor, pedreiro, técnico, designer, fotógrafo, diarista e beleza/manicure alimentam o fluxo de orçamento guiado.
- Criar orçamento por modelo: acessar `/Templates`, escolher um card e clicar em “Usar este modelo”, ou abrir `/Documents/CreateBudget?templateId=...`.
- Checklist mobile: sem overflow horizontal, menu acessível, cards empilhados, links visíveis e foco aparente por teclado.
