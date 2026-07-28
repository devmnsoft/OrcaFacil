# Causa raiz da falha de cadastro

## Evidência disponível

O identificador `0HNNCL8Q8175V:0000002F` veio do relato e não existe nos logs versionados do repositório. Portanto, ele **não foi apresentado como log consultado**. A investigação reproduzível foi feita comparando o modelo gravado pelo cadastro, as configurações EF, as migrations e `database/script_completop.sql`.

## Causa confirmada pela análise estrutural

O cadastro prepara uma `Subscription` com `SelectedPlanVersionId`, `EffectivePlanVersionId`, `PriceAtActivation` e campos de trial. Nenhuma migration existente até `20260727000000_ConsolidateAccountIsolation` criava `plans`, `plan_versions`, `business_accounts` ou `account_members`; essa migration, inclusive, já consultava `account_members`. O script consolidado criava parte dessas tabelas, mas não criava as colunas de versão da assinatura.

Assim, havia dois caminhos de falha, conforme a origem do banco:

| Banco | Etapa | Falha PostgreSQL esperada e determinística | Objeto |
|---|---|---|---|
| Criado pelas migrations | consulta do plano FREE | `42P01` (`undefined_table`) | `orcafacil.plans` ou `orcafacil.account_members` durante a migration anterior |
| Criado por `script_completop.sql` | `REGISTER_SAVE_STARTED` | `42703` (`undefined_column`) | `orcafacil.subscriptions.selected_plan_version_id` (e demais colunas ausentes) |

Não há dump do banco real nem evento estruturado correspondente ao correlationId fornecido; por isso não é correto inventar mensagem de exceção, constraint ou tabela observada naquele evento. A causa estrutural acima é verificável no código anterior à correção.

## Correção

- A migration `20260728000000_StabilizeRegistrationSchema` cria de modo idempotente as tabelas de conta e catálogo ausentes, completa `subscriptions` e `notifications` e publica a versão FREE inicial.
- O script consolidado passou a criar as mesmas colunas e FKs.
- O cadastro agora abre uma transação explícita antes de anexar as nove gravações atuais ao mesmo `DbContext`, salva uma vez, confirma depois do `SaveChanges` e limpa o tracker no rollback.
- Logs estruturados registram cada etapa sem senha ou documento.
- O mapeamento público diferencia duplicidade de e-mail/documento por constraint e mantém detalhes de infraestrutura apenas no log.

## Prevenção de regressão

Aplicar a migration em banco descartável e executar os cenários PF/PJ. O CI deve executar build/testes em .NET 10; este ambiente de trabalho não continha o executável `dotnet`, portanto a execução local está registrada como limitação, não como sucesso.
