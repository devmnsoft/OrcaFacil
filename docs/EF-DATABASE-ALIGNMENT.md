# Alinhamento EF Core e banco de dados

Auditoria estática de 27/07/2026. **Não validado** significa que o SDK/PostgreSQL não estavam disponíveis para gerar script idempotente e comparar o modelo em execução.

| Entidade | DbSet | Configuração/tabela | Migration e SQL | Índices/FKs | Status |
|---|---|---|---|---|---|
| BusinessAccount | `BusinessAccounts` | `BusinessAccountConfiguration` / `business_accounts` | inicial + script completo | documento único, status | Parcial |
| AccountMember | `AccountMembers` | `AccountMemberConfiguration` / `account_members` | inicial + script | conta/usuário | Parcial |
| BillingCustomerProfile | `BillingCustomerProfiles` | configuração própria | inicial + script | conta/usuário | Parcial |
| Plan, PlanVersion | `Plans`, `PlanVersions` | configurações comerciais | consolidação + script | código; plano/versão | Parcial |
| Feature, PlanFeatureValue | `Features`, `PlanFeatureValues` | configurações comerciais | consolidação + script | código; versão/feature | Parcial |
| Subscription | `Subscriptions` | `SubscriptionConfiguration` | inicial/consolidação + script | conta e versões precisam validação | Parcial |
| BillingInvoice | `BillingInvoices` | configuração comercial | consolidação + script | conta/assinatura | Não validado |
| Payment, PaymentEvent | `Payments`, `PaymentEvents` | configurações próprias | isolamento aditivo + script | conta, fatura, idempotência | Parcial |
| PlanOverride, SubscriptionEvent | DbSets próprios | configuração comercial | consolidação + script | conta/versão/assinatura | Parcial |
| ActivityEvent, SupportAccessSession | DbSets próprios | configuração comercial | consolidação + script | conta e datas | Parcial |
| Client | `Clients` | `ClientConfiguration` | isolamento aditivo + script | `account_id` | Parcial |
| ServiceCatalogItem | ausente | ausente | ausente | ausentes | Quebrado |
| Document | `Documents` | `DocumentConfiguration` | isolamento aditivo + script | conta/criação e conta/cliente | Parcial |
| PublicQuote | `PublicQuotes` | `PublicQuoteConfiguration` | isolamento aditivo + script | conta/criação | Parcial |
| UserUsage | `UserUsage` | `UserUsageConfiguration` | isolamento aditivo + script | conta/período | Parcial |

## Divergências confirmadas

- `OrcaFacilDbContextModelSnapshot.cs` não contém o modelo; ele deve ser regenerado por uma migration aditiva antes da publicação.
- `ServiceCatalogItem` ainda não existe, e a utilização de serviços continua sendo inferida de eventos de atividade.
- As colunas de transição de assinatura permanecem opcionais para preservar dados até o backfill; constraints só podem ser adicionadas após relatório de inconsistências.
- A migration usa schema explícito `orcafacil`, enquanto configurações comerciais confiam no schema padrão. Isso é coerente em execução, mas deve ser comprovado por script gerado pelo EF.

## Procedimento obrigatório de fechamento

Executar `dotnet ef migrations script --idempotent`, aplicar em PostgreSQL vazio e em cópia anonimizada, comparar tabelas/índices/FKs com `database/script_completop.sql` e somente então classificar os itens como **Concluído**.
