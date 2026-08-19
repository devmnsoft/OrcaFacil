# Operação assistida pós-go-live V1

## Rotina diária

- [ ] Conferir `/health` e Diagnóstico: conexão, schema esperado e dependências, sem dados sensíveis.
- [ ] Revisar últimos erros e correlações em Logs; abrir incidente para recorrência ou severidade crítica.
- [ ] Revisar EmailOutbox: pendentes antigos, falhos e tentativas; reprocessar somente após corrigir a causa.
- [ ] Investigar falhas de login anormais, bloqueios e sinais de tentativa automatizada.
- [ ] Confirmar conclusão, tamanho e retenção do backup datado; ensaiar restore periodicamente em destino isolado.
- [ ] Amostrar propostas públicas válidas/expiradas e decisões, sem copiar tokens para chamados.
- [ ] Conciliar pagamentos, reversões e recibos; investigar duplicidade e confirmar o aviso fiscal.
- [ ] Conferir alertas comerciais/financeiros e ausência de geração duplicada.
- [ ] Acompanhar limites e uso do plano, contas suspensas e trials próximos do vencimento.
- [ ] Tratar chamados abertos por prioridade, prazo e conta, sem incluir segredo no histórico.

## Painel SuperAdmin

O operador autorizado acompanha Logs, EmailOutbox, Suporte, Contas/Assinaturas, uso e Diagnóstico. A revisão diária deve cobrir últimos erros, e-mails falhos, chamados abertos, contas suspensas, trials vencendo, uso do sistema e schema desatualizado. Acesso de suporte deve ser mínimo, temporário e auditado.

## Escalonamento

- **P0:** interromper deploy/operação afetada, preservar evidências e iniciar rollback.
- **P1:** mitigar no mesmo turno, bloquear a ação afetada de forma honesta e preparar correção versionada.
- **P2/P3:** registrar backlog sem desviar a observação de segurança e integridade.

Registre diariamente responsável, período observado, contagens, incidentes, decisão e links internos de evidência. Nunca registre senha, token, cookie, chave, payload protegido ou connection string completa.
