# Reparo do schema do perfil de cobrança

## Causa e impacto

A migration `20260728000000_StabilizeRegistrationSchema` criava
`billing_customer_profiles` com `mercado_pago_customer_id`, mas seu caminho para tabelas já
existentes não adicionava essa coluna. Como `CREATE TABLE IF NOT EXISTS` não modifica uma tabela,
instalações preexistentes podiam falhar no cadastro com PostgreSQL `42703`. A transação de cadastro
é atômica; não se deve apagar automaticamente eventual dado órfão histórico.

## Correção

A migration aditiva `20260728210000_RepairBillingCustomerProfileSchema` audita o conjunto esperado
de colunas por meio de `ADD COLUMN IF NOT EXISTS` e adiciona
`mercado_pago_customer_id varchar(180) NULL`, sem valor padrão, FK ou chamada ao Mercado Pago. O
`Down` é intencionalmente não destrutivo. O script consolidado contém o mesmo reparo idempotente.

## Aplicação

```bash
dotnet ef migrations list --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
```

Execute em cada banco que já tenha aplicado a estabilização e também na criação de bancos novos.
Faça backup e registre a janela de mudança antes da aplicação em produção.

## Validação

```sql
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'orcafacil' AND table_name = 'billing_customer_profiles'
ORDER BY ordinal_position;
```

A linha `mercado_pago_customer_id | character varying | 180 | YES` deve existir. Confirme também
`/health/ready`: a resposta pública expõe somente estado e correlation ID; divergências pertencem
ao diagnóstico administrativo. Cadastros PF e PJ precisam terminar em
`REGISTER_TRANSACTION_COMMITTED` antes da liberação.

## Rollback não destrutivo

Não remova a coluna: ela é anulável e inócua para contas gratuitas. Em caso de incidente, reverta a
versão da aplicação, preserve os dados e registre o resultado do contrato de schema. Qualquer órfão
deve ser relatado e corrigido por procedimento administrativo auditado, nunca excluído em massa.
