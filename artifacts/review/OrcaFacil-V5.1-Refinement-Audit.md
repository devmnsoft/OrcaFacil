# OrçaFácil V5.1 — Auditoria de Refinamento

Auditoria executada exclusivamente sobre a solução ASP.NET atual. A classificação segue P0 (execução/segurança), P1 (fluxo/regra), P2 (consistência/UX) e P3 (apresentação/produtividade).

| Módulo | Tela/serviço | Problema encontrado | Severidade | Regra afetada | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|
| Transversal | Jornadas | Não existia uma visão consolidada e determinística por jornada | P2 | Scores devem vir de checks reais | `UserJourneyReviewService` agrega os checks da Central de Qualidade, sem aleatoriedade | Persistir histórico apenas quando houver requisito operacional de comparação temporal |
| Comercial | Orçamento/proposta | Pré-condições de avanço não estavam reunidas em contrato testável | P1 | Orçamento exige cliente/item; proposta expirada exige reabertura | Validações explícitas em `JourneyRuleValidationService` e regressões unitárias | Integrar gradualmente nos handlers legados após homologação dos fluxos atuais |
| Operacional | Conclusão da OS | Mensagem conhecida de checklist precisava orientar a correção | P2 | Checklist obrigatório bloqueia conclusão | Catálogo de mensagens amigáveis oferece orientação objetiva | Conectar o código ao handler de campo durante a próxima revisão funcional ponta a ponta |
| Financeiro | Recibo | Erro de pagamento pendente precisava de orientação consistente | P2 | Pagamento pendente não gera recibo | Mensagem amigável padronizada; regra de recibo existente preservada | Nenhuma no contrato de aplicação |
| Fiscal | Emissão | Falta de prontidão fiscal precisava indicar a ação necessária | P2 | Empresa e cliente devem estar fiscalmente prontos | Mensagem amigável específica adicionada | Validação online continua dependente de provedor fiscal configurado |
| Portais | Visibilidade | Qualidade dos portais precisava compor a revisão por jornada | P1 | Isolamento por conta/cliente/parceiro | Central reaproveita checks reais e guardas existentes | Teste autenticado com banco real depende do ambiente operacional |
| Administração | Permissões/menu | Central V5.1 não possuía permissão nem rota protegida | P1 | URL e menu precisam de autorização | Permissões idempotentes, policy no PageModel e item de navegação condicionado | Executar migration no ambiente de destino |
| UX | Central de Refinamento | Faltavam empty state, progresso acessível e próxima ação | P3 | Tela crítica deve orientar o usuário | Razor responsivo reaproveita o design system de qualidade | Validação visual em navegador requer runtime .NET disponível |

## Varreduras e bloqueios

- A busca por fallbacks inválidos encontrou apenas detectores, diagnósticos e casos de teste intencionais; não foi identificado fallback silencioso de produção.
- A central calcula seus percentuais pela razão entre controles aprovados e controles avaliados.
- Nenhuma tabela foi removida ou recriada; a migration apenas insere permissões de forma idempotente.
