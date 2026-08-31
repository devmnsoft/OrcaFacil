# OrçaFácil V4.8 — pré-auditoria financeira

Data: 2026-08-31. Escopo: somente a solução ASP.NET atual.

## Resultado

- A busca por fallbacks `Database=unavailable`, `127.0.0.1:1`, `Port=1` e segredos não identificou novo fallback silencioso no módulo entregue.
- O financeiro V2.5 existente já possuía contas bancárias, contas a pagar, movimentos, conciliação, rateio e bloqueio de período; a V4.8 foi construída de forma aditiva sobre essas regras.
- Valores monetários novos usam `decimal`; projeções e relatórios são determinísticos e recebem fatos persistidos, sem geradores aleatórios ou dados demonstrativos.
- Todo novo registro persistente possui `account_id`; índices de unicidade e consulta incluem o escopo da conta.
- Lançamentos manuais, ajustes, reaberturas e versões preservam motivo, ator e histórico de auditoria.

## Riscos e decisões

- A DRE permanece explicitamente gerencial; não substitui escrituração contábil.
- A migration é aditiva e seu `Down` é intencionalmente não destrutivo.
- Validação visual/browser depende de uma instância configurada com PostgreSQL e usuário autorizado; nenhum dado financeiro foi criado para simular essa validação.
