# Roadmap

## Concluído no MVP navegável

- Login, cadastro e logout Web com cookie.
- Dashboard, perfil do emitente, criação de orçamento/recibo, histórico, detalhes, edição, duplicação, exclusão lógica e PDF.
- Aprovação pública por `/p/{token}`.
- Admin Geral básico.
- Script de publicação IIS.

## Pendências pós-MVP

- Mercado Pago, nota fiscal, WhatsApp oficial, BI avançado, assinatura ICP-Brasil e recursos de IA paga.

## UX/UI premium MVP

- Design system, layouts público/autenticado e componentes reutilizáveis implementados.
- Próximos passos: persistir filtros avançados, completar recusa pública com auditoria dedicada, evoluir upload de logo e ativação Pro automatizada.

## Evolução SaaS premium: modelos por profissão e orçamento guiado

- Contraste: páginas devem usar `.of-section-light` para fundo claro com texto escuro e `.of-section-dark` para fundo escuro com texto branco/alta opacidade.
- Rodapé premium: layout público em três colunas no desktop, empilhado no mobile, com marca OrçaFácil, links institucionais e dados MNSOFT sem overflow.
- Link `/#recursos`: a seção de recursos da landing possui `id="recursos"`; validar clique no menu público e scroll suave.
- Modelos por profissão: eletricista, pintor, pedreiro, técnico, designer, fotógrafo, diarista e beleza/manicure alimentam o fluxo de orçamento guiado.
- Criar orçamento por modelo: acessar `/Templates`, escolher um card e clicar em “Usar este modelo”, ou abrir `/Documents/CreateBudget?templateId=...`.
- Checklist mobile: sem overflow horizontal, menu acessível, cards empilhados, links visíveis e foco aparente por teclado.
