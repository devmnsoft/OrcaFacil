# Inventário do estado atual após o PR 56

Levantamento realizado em 27/07/2026 sobre o commit `767bb2c`. Os estados abaixo descrevem comportamento comprovável no branch; uma entidade ou uma página sem fluxo completo não foi classificada como concluída.

| Módulo | Arquivos principais | Domínio e persistência | Interface e segurança | Testes | Status e pendências |
|---|---|---|---|---|---|
| Cadastro | `AuthService`, `Pages/Auth/Register.*` | PF/PJ, CPF/CNPJ, conta, membro, perfil de cobrança e assinatura são criados; a versão FREE publicada é exigida | formulário e transação existentes; falha amigável ainda deve ser validada em PostgreSQL | contratos estáticos | **Parcial** — falta teste transacional real |
| Login | `Pages/Auth/Login.*`, `CurrentAccountService` | membro ativo é resolvido | claims de conta são emitidas; múltiplas contas não têm seletor | não específico | **Parcial** — criar `/Account/Choose` e validador de sessão dedicado |
| Conta | `BusinessAccount`, `AccountMember`, `CurrentAccountService` | conta e vínculo persistidos | autorização revalida membro/conta no banco | cobertura de bloqueio | **Parcial** — ampliar contrato e policies por permissão |
| Membros | `AccountMember`, tabelas de roles/permissões | estrutura persistida | não há gestão completa | ausente | **Estrutura** |
| Clientes cadastrados | `Client`, `Pages/Clients` | `AccountId` existe; CRUD presente | consultas por conta em evolução | `ClientPageTests` | **Parcial** — tags e limites precisam de cobertura integrada |
| Serviços | `Pages/Services/Index.cshtml` | não existe `ServiceCatalogItem` persistido | página sem CRUD real | ausente | **Placeholder** |
| Documentos | `Document`, `DocumentItem`, `Pages/Documents` | `AccountId` e `ClientId` existem; snapshot precisa de validação integral | fluxos de orçamento/recibo presentes | domínio/queries | **Parcial** |
| PDF | `QuestPdfDocumentService`, templates QuestPDF | geração persistente contabilizada em `UserUsage` | rotas existentes | testes específicos ausentes | **Parcial** |
| Modelos | `BudgetTemplate`, `BudgetTemplateItem`, `Pages/Templates` | modelo de sistema/conta existe | telas existentes | ausente | **Parcial** — remover semântica legada `templates.basic_limit` |
| Planos e benefícios | `PlanCatalog`, `PlanAccessService`, `EfPlanAccessDataSource` | catálogo versionado no banco e `IsUnlimited`; fonte de execução é EF | decisão no backend | `CommercialPlatformTests` | **Parcial** — completar seed e administração de versões |
| Assinaturas | `Subscription`, `SubscriptionEvent`, migration de consolidação | vínculos de versão e conta opcionais na transição | página “Meu plano” ainda sem PageModel | fallback testado em memória | **Parcial** |
| Faturas e pagamentos | `BillingInvoice`, `Payment`, `PaymentEvent` | estrutura existente | gateway preservado; não há sucesso fictício criado nesta etapa | ausente | **Estrutura** |
| SuperAdministrador | `SuperAdminSeeder`, `AdminService`, `Pages/Admin/Index` | usuário de plataforma e consultas parciais | dashboard parcial, sem telas completas de contas/planos | contratos estáticos | **Parcial** |
| Notificações | `NotificationService`, `Pages/Notifications` | persistência existente | lista existente | ausente | **Parcial** |
| Auditoria e atividade | `AuditLog`, `ActivityEvent`, `AuditService` | tabelas e serviços presentes | sem consulta operacional completa | ausente | **Estrutura** |
| Suporte | `SupportAccessSession`, `Pages/Support/Index` | sessão limitada a 30 minutos no domínio | página sem jornada administrativa completa | domínio indireto | **Estrutura** |
| Configurações | `AdminSetting`, `Pages/Profile` | configurações e perfil persistidos | interface parcial | ausente | **Parcial** |
| Layout e acessibilidade | layouts, partials, CSS e scripts de QA | não aplicável | componentes e foco parcial; há partials duplicados em `Shared`/`Shared/Partials` | scripts estáticos | **Duplicado** — consolidar partials e design system |
| Migrations | três migrations e snapshot | migration aditiva de isolamento; snapshot vazio | não aplicável | `DbContextContractTests` | **Quebrado** — regenerar snapshot e validar contra PostgreSQL |
| Testes | `tests/OrcaFacil.UnitTests` | cobertura majoritariamente unitária/estática | sem suíte browser | `dotnet test` indisponível no container | **Não validado** |

## Riscos que bloqueiam a classificação de release candidate

1. O container não contém o SDK .NET 10, portanto compilação e testes não foram certificados localmente.
2. O `ModelSnapshot` vazio impede afirmar alinhamento completo entre migrations e o modelo atual.
3. Catálogo de serviços, seletor de contas e administração operacional ainda não estão concluídos.
4. A validação de integração depende de PostgreSQL com o schema `orcafacil` aplicado.
