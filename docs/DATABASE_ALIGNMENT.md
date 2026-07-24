# Database Alignment — schema `orcafacil`

| Entidade | Tabela | Configuration | Migration | Dapper/Queries | Script SQL | Status |
|---|---|---|---|---|---|---|
| `UserAccount` | `orcafacil.users` | `UserAccountConfiguration` | Sim | Dashboard/Admin | Sim | Parcial |
| `IssuerProfile` | `orcafacil.issuer_profiles` | `IssuerProfileConfiguration` | Sim | PDF/Profile | Sim | Parcial |
| `Client` | `orcafacil.clients` | `ClientConfiguration` | Sim | Document/Admin | Sim | Parcial |
| `ServiceCatalogItem` | `orcafacil.service_catalog_items` | Não encontrada | Não validado | Não encontrado | Sim | Stub |
| `BudgetTemplate` | `orcafacil.budget_templates` | `BudgetTemplateConfiguration` | Sim | Não validado | Sim | Parcial |
| `BudgetTemplateItem` | `orcafacil.budget_template_items` | `BudgetTemplateItemConfiguration` | Sim | Não validado | Sim | Parcial |
| `Document` | `orcafacil.documents` | `DocumentConfiguration` | Sim | `DocumentQueries` | Sim | Parcial |
| `DocumentItem` | `orcafacil.document_items` | `DocumentItemConfiguration` | Sim | `DocumentQueries` | Sim | Parcial |
| `PublicQuote` | `orcafacil.public_quotes` | `PublicQuoteConfiguration` | Sim | Document/public | Sim | Parcial |
| `DocumentEvent` | `orcafacil.document_events` | Não encontrada | Não validado | Não encontrado | Sim | Stub |
| `DocumentVersion` | `orcafacil.document_versions` | Não encontrada | Não validado | Não encontrado | Sim | Stub |
| `FollowUpTask` | `orcafacil.follow_up_tasks` | Não encontrada | Não validado | Não encontrado | Sim | Stub |
| `Subscription` | `orcafacil.subscriptions` | `SubscriptionConfiguration` | Sim | Billing/Admin | Sim | Parcial |
| `Payment` | `orcafacil.payments` | `PaymentConfiguration` | Sim | Admin/Billing | Sim | Parcial |
| `PaymentEvent` | `orcafacil.payment_events` | `PaymentEventConfiguration` | Sim | Não validado | Sim | Parcial |
| `BillingCustomerProfile` | `orcafacil.billing_customer_profiles` | `BillingCustomerProfileConfiguration` | Sim | Billing | Sim | Parcial |
| `PlanFeature` | `orcafacil.plan_features` | `PlanFeatureConfiguration` | Sim | Plans | Sim | Parcial |
| `Notification` | `orcafacil.notifications` | `NotificationConfiguration` | Sim | NotificationService | Sim | Parcial |
| `AccountDeletionRequest` | `orcafacil.account_deletion_requests` | Não encontrada | Não validado | Não encontrado | Sim | Stub |
| `AuditLog` | `orcafacil.audit_logs` | `AuditLogConfiguration` | Sim | Admin/logs | Sim | Parcial |
| `SystemLog` | `orcafacil.system_logs` | `SystemLogConfiguration` | Sim | Admin/logs | Sim | Parcial |
| `SystemError` | `orcafacil.system_errors` | `SystemErrorConfiguration` | Sim | Admin/errors | Sim | Parcial |
| `MercadoPagoWebhookEvent` | `orcafacil.mercado_pago_webhook_events` | `MercadoPagoWebhookEventConfiguration` | Sim | Webhook endpoint | Sim | Parcial |

## Achados

- O `DbContext` define schema padrão `orcafacil`, alinhado à regra de schema único.
- O script SQL contém tabelas de funcionalidades ainda não integradas no ASP.NET (`service_catalog_items`, `document_events`, `document_versions`, `follow_up_tasks`, `account_deletion_requests`). Elas devem permanecer marcadas como stub até existir uso funcional real ou ser removidas do script de release.
- Validação de migrations e execução do SQL não foram possíveis porque o SDK .NET não está instalado neste ambiente.
