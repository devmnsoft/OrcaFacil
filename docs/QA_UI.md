# QA UI

- [ ] Cadastro legível em desktop e mobile.
- [ ] Login legível em desktop e mobile.
- [ ] Sem texto branco em fundo claro.
- [ ] Sem footer duplicado.
- [ ] `/diagnostico` não é público; exige SuperAdmin.
- [ ] Toasts funcionando para cadastro, login, perfil e documentos.
- [ ] Mobile sem overflow horizontal.
- [ ] SVGs textuais carregando sem binários.
- [ ] Ícones Bootstrap Icons visíveis.

## Checklist visual desta etapa

- [ ] Cadastro tem contraste adequado.
- [ ] Senha inicia oculta.
- [ ] Olhinho mostra/oculta senha.
- [ ] Confirmar senha igual ao campo senha.
- [ ] Nenhum texto branco em fundo claro.
- [ ] Erro de banco mostra mensagem amigável.
- [ ] Stack trace não aparece para usuário.
- [ ] SVGs estão no contexto de orçamento/recibo/PDF.
- [ ] Mobile não quebra.

## Evolução SaaS premium: modelos por profissão e orçamento guiado

- Contraste: páginas devem usar `.of-section-light` para fundo claro com texto escuro e `.of-section-dark` para fundo escuro com texto branco/alta opacidade.
- Rodapé premium: layout público em três colunas no desktop, empilhado no mobile, com marca OrçaFácil, links institucionais e dados MNSOFT sem overflow.
- Link `/#recursos`: a seção de recursos da landing possui `id="recursos"`; validar clique no menu público e scroll suave.
- Modelos por profissão: eletricista, pintor, pedreiro, técnico, designer, fotógrafo, diarista e beleza/manicure alimentam o fluxo de orçamento guiado.
- Criar orçamento por modelo: acessar `/Templates`, escolher um card e clicar em “Usar este modelo”, ou abrir `/Documents/CreateBudget?templateId=...`.
- Checklist mobile: sem overflow horizontal, menu acessível, cards empilhados, links visíveis e foco aparente por teclado.
