# QA Database - OrçaFácil PostgreSQL

Checklist de homologação do banco padronizado no schema único `orcafacil`.

- [ ] PostgreSQL instalado
- [ ] Banco `orcafacil` criado
- [ ] Usuário `orcafacil_user` criado
- [ ] `database/script_completop.sql` executou sem erro
- [ ] Schema `orcafacil` existe
- [ ] Tabelas principais existem:
  - [ ] `orcafacil.users`
  - [ ] `orcafacil.issuer_profiles`
  - [ ] `orcafacil.documents`
  - [ ] `orcafacil.document_items`
  - [ ] `orcafacil.public_quotes`
  - [ ] `orcafacil.user_usage`
  - [ ] `orcafacil.subscriptions`
  - [ ] `orcafacil.payments`
  - [ ] `orcafacil.admin_settings`
  - [ ] `orcafacil.notifications`
  - [ ] `orcafacil.audit_logs`
  - [ ] `orcafacil.system_logs`
  - [ ] `orcafacil.system_errors`
- [ ] Seeds `admin_settings` existem
- [ ] App conecta no banco
- [ ] `/health/db` retorna OK
- [ ] Admin > Banco mostra OK
- [ ] Cria usuário
- [ ] Cria perfil
- [ ] Cria orçamento
- [ ] Cria recibo
- [ ] Gera PDF

## Execução manual

```bash
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

O script é idempotente e seguro para reexecução: usa `CREATE SCHEMA IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS` e `INSERT ... ON CONFLICT`.

## Checklist rápido para erro 28P01

1. Confirme a variável `ConnectionStrings__DefaultConnection`.
2. Teste com `psql "Host=localhost;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=<informada-localmente>"`.
3. Se falhar, rode `ALTER USER orcafacil_user WITH PASSWORD '123456';` como administrador.
4. Acesse `/health/db` e `/Admin/Settings/Database` para validar o status sem expor stack trace.
