# Mapa de relacionamentos do cadastro

Este mapa compara o modelo EF com o esquema consolidado em `database/script_completop.sql`. Os nomes automáticos foram derivados das declarações `REFERENCES`; a consulta de `pg_constraint` abaixo deve ser executada em cada ambiente antes de qualquer renomeação de constraint.

| Dependente | FK | Principal | Constraint no script | Nullable | DeleteBehavior EF | EF | PostgreSQL/script |
|---|---|---|---|---:|---|---|---|
| account_members | account_id | business_accounts.id | account_members_account_id_fkey | não | Restrict | mapeada | presente |
| account_members | user_id | users.id | account_members_user_id_fkey | não | Restrict | mapeada | presente |
| billing_customer_profiles | account_id | business_accounts.id | billing_customer_profiles_account_id_fkey | não na entidade | Restrict | mapeada 1:1 | presente |
| billing_customer_profiles | user_id | users.id | fk_orcafacil_billing_customer_profiles_users | não | Restrict | mapeada | presente |
| subscriptions | account_id | business_accounts.id | subscriptions_account_id_fkey | sim | Restrict | mapeada | presente |
| subscriptions | user_id | users.id | fk_orcafacil_subscriptions_users | não | Restrict | mapeada | presente |
| subscriptions | selected_plan_version_id | plan_versions.id | fk_subscriptions_selected_plan_version | sim | Restrict | mapeada | presente |
| subscriptions | effective_plan_version_id | plan_versions.id | fk_subscriptions_effective_plan_version | sim | Restrict | mapeada | presente |
| issuer_profiles | user_id | users.id | fk_orcafacil_issuer_profiles_users | não | Restrict | mapeada 1:1 | presente |
| notifications | account_id | business_accounts.id | notifications_account_id_fkey | sim | Restrict | mapeada | presente |
| notifications | user_id | users.id | fk_orcafacil_notifications_users | não | Restrict | mapeada | presente |
| notifications | document_id | documents.id | — | sim | — | não mapeada | FK ausente |
| audit_logs | account_id | business_accounts.id | — | sim | — | não mapeada | coluna/FK ausente |
| audit_logs | user_id | users.id | — | sim | — | não mapeada | FK ausente |

Não foi criada migration: as relações necessárias ao cadastro já existem, e `audit_logs.account_id` exige uma evolução separada com coluna, backfill e política de retenção. Mapear relações inexistentes produziria divergência entre o EF e instalações atuais.

## Verificação em PostgreSQL

```sql
SELECT con.conname, rel.relname AS tabela, frel.relname AS tabela_referenciada,
       pg_get_constraintdef(con.oid)
FROM pg_constraint con
JOIN pg_class rel ON rel.oid = con.conrelid
LEFT JOIN pg_class frel ON frel.oid = con.confrelid
JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
WHERE nsp.nspname = 'orcafacil' AND con.contype = 'f'
ORDER BY rel.relname, con.conname;
```
