# Consolidação do banco por conta

## Decisões

- `BusinessAccount.Id` é a unidade de isolamento. `UserId` permanece como autoria e compatibilidade, nunca como autorização entre contas.
- A fonte de benefícios em execução é `plans` → `plan_versions` → `plan_feature_values`/`features`, selecionada por `subscriptions` e, temporariamente, `plan_overrides`.
- Uma liberação temporária referencia uma versão real. Ao expirar ou ser revogada, a resolução volta automaticamente à versão efetiva da assinatura.
- Documentos passam a guardar `ClientId` para relacionamento e continuam guardando nome, documento, e-mail, telefone e cidade como fotografia histórica.
- Pagamentos passam a aceitar a FK `BillingInvoiceId`; pagamentos manuais legados continuam possíveis durante a transição e devem ser auditados.

## Migração aditiva

`20260727000000_ConsolidateAccountIsolation` cria colunas ausentes, preenche `AccountId` por meio do membro Owner, propaga a conta do documento para links públicos e cria índices. Ela não remove colunas nem dados e deixa o `Down` vazio deliberadamente.

Antes de tornar os campos obrigatórios em uma etapa futura, a operação deve comparar contagens de linhas totais e linhas com `account_id`, resolver membros legados sem Owner e só então criar FKs adicionais.

## Compatibilidade a remover futuramente

`UserAccount.Plan`, `Subscription.Plan` e `BusinessAccount.CurrentPlanCode` permanecem apenas como resumos legados. Nenhum fluxo novo deve usá-los para liberar benefícios; a decisão oficial é feita por `IPlanAccessService` com `AccountId`.
