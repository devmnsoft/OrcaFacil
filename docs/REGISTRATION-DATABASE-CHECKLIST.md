# Checklist do banco para cadastro

Schema obrigatório: `orcafacil`. Produção exige migration controlada; a aplicação não deve migrar automaticamente no startup.

| Tabela | Chave/relacionamento crítico | Estado após estabilização |
|---|---|---|
| `users` | e-mail único; nome e hash obrigatórios | Alinhada |
| `business_accounts` | documento único apenas quando não excluído | Alinhada pela migration 20260728000000 |
| `account_members` | FK conta/usuário; único `(account_id,user_id)` | Alinhada pela migration 20260728000000 |
| `billing_customer_profiles` | FK de conta; conta e documento únicos | Colunas aditivas alinhadas |
| `subscriptions` | FKs para conta e versões selecionada/efetiva; preço zero permitido | Colunas e FKs alinhadas |
| `plans` | `code` único; `FREE` ativo | Alinhada e seed idempotente |
| `plan_versions` | único `(plan_id,version_number)`; versão FREE publicada e vigente | Alinhada e seed idempotente |
| `notifications` | FK usuário; conta opcional; categoria e ação | Colunas alinhadas |
| `audit_logs` | incluído no mesmo `DbContext`/commit do cadastro | Alinhada; sem `SaveChanges` próprio |
| `issuer_profiles` | FK e índice único de usuário | Alinhada |

## Comandos de desenvolvimento

```bash
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
dotnet ef migrations list --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

Antes de produção: backup, revisão do SQL gerado, janela aprovada, execução com usuário de migration e validação de readiness. Não conceder permissão DDL ao usuário normal da aplicação.
