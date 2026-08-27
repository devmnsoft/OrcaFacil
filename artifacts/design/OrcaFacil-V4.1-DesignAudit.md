# OrçaFácil V4.1 — Auditoria de Design

**Escopo:** Razor Pages do projeto ASP.NET atual. **Data:** 2026-08-27.

## Critérios

- **P0:** jornada crítica, pública, autenticação, dashboard, portais ou offline.
- **P1:** operação recorrente que exige consistência enterprise.
- **P2:** funcional e coberta pelo design system, com polimento incremental.
- **P3:** não atribuído nesta auditoria; todas as rotas permanecem no backlog de verificação visual.

## Inventário por área

### Admin

- **P1** — `/Admin` (`Pages/Admin/Index.cshtml`)

### Público

- **P2** — `/Ajuda` (`Pages/Ajuda.cshtml`)
- **P2** — `/Cadastro` (`Pages/Cadastro.cshtml`)
- **P2** — `/Comecar` (`Pages/Comecar.cshtml`)
- **P2** — `/ComoFunciona` (`Pages/ComoFunciona.cshtml`)
- **P2** — `/CondicoesComerciais` (`Pages/CondicoesComerciais.cshtml`)
- **P2** — `/Contato` (`Pages/Contato.cshtml`)
- **P2** — `/Cookies` (`Pages/Cookies.cshtml`)
- **P2** — `/Demo` (`Pages/Demo.cshtml`)
- **P2** — `/Diagnostico` (`Pages/Diagnostico.cshtml`)
- **P2** — `/Discover` (`Pages/Discover.cshtml`)
- **P2** — `/DomainUnavailable` (`Pages/DomainUnavailable.cshtml`)
- **P2** — `/Emitente` (`Pages/Emitente.cshtml`)
- **P2** — `/Error` (`Pages/Error.cshtml`)
- **P2** — `/Historico` (`Pages/Historico.cshtml`)
- **P2** — `/Implantacao` (`Pages/Implantacao.cshtml`)
- **P0** — `/Index` (`Pages/Index.cshtml`)
- **P2** — `/Integracoes` (`Pages/Integracoes.cshtml`)
- **P2** — `/LGPD` (`Pages/LGPD.cshtml`)
- **P2** — `/Login` (`Pages/Login.cshtml`)
- **P0** — `/Offline` (`Pages/Offline.cshtml`)
- **P2** — `/Precos` (`Pages/Precos.cshtml`)
- **P2** — `/Privacidade` (`Pages/Privacidade.cshtml`)
- **P2** — `/Seguranca` (`Pages/Seguranca.cshtml`)
- **P2** — `/Sobre` (`Pages/Sobre.cshtml`)
- **P2** — `/Status` (`Pages/Status.cshtml`)
- **P2** — `/Termos` (`Pages/Termos.cshtml`)
- **P2** — `/_ViewImports` (`Pages/_ViewImports.cshtml`)
- **P2** — `/_ViewStart` (`Pages/_ViewStart.cshtml`)

### Alerts

- **P2** — `/Alerts` (`Pages/Alerts/Index.cshtml`)

### Analytics

- **P1** — `/Analytics/AccountHealth` (`Pages/Analytics/AccountHealth.cshtml`)
- **P1** — `/Analytics/DataQuality` (`Pages/Analytics/DataQuality.cshtml`)
- **P1** — `/Analytics/Executive` (`Pages/Analytics/Executive.cshtml`)
- **P1** — `/Analytics/Forecast` (`Pages/Analytics/Forecast.cshtml`)

### Approvals

- **P2** — `/Approvals` (`Pages/Approvals/Index.cshtml`)

### Assistant

- **P2** — `/Assistant` (`Pages/Assistant/Index.cshtml`)

### Auth

- **P2** — `/Auth/ForgotPassword` (`Pages/Auth/ForgotPassword.cshtml`)
- **P2** — `/Auth/ForgotPasswordConfirmation` (`Pages/Auth/ForgotPasswordConfirmation.cshtml`)
- **P0** — `/Auth/Login` (`Pages/Auth/Login.cshtml`)
- **P2** — `/Auth/Register` (`Pages/Auth/Register.cshtml`)
- **P2** — `/Auth/ResetPassword` (`Pages/Auth/ResetPassword.cshtml`)
- **P2** — `/Auth/ResetPasswordConfirmation` (`Pages/Auth/ResetPasswordConfirmation.cshtml`)
- **P2** — `/Auth/_ViewStart` (`Pages/Auth/_ViewStart.cshtml`)

### Blog

- **P2** — `/Blog` (`Pages/Blog/Index.cshtml`)

### CashFlow

- **P1** — `/CashFlow` (`Pages/CashFlow/Index.cshtml`)

### Clients

- **P1** — `/Clients/Create` (`Pages/Clients/Create.cshtml`)
- **P1** — `/Clients/Details` (`Pages/Clients/Details.cshtml`)
- **P1** — `/Clients/Edit` (`Pages/Clients/Edit.cshtml`)
- **P1** — `/Clients` (`Pages/Clients/Index.cshtml`)
- **P1** — `/Clients/_Editor` (`Pages/Clients/_Editor.cshtml`)

### CommandCenter

- **P2** — `/CommandCenter` (`Pages/CommandCenter/Index.cshtml`)

### CommercialPipeline

- **P2** — `/CommercialPipeline` (`Pages/CommercialPipeline/Index.cshtml`)

### CommercialRoutine

- **P2** — `/CommercialRoutine` (`Pages/CommercialRoutine/Index.cshtml`)
- **P2** — `/CommercialRoutine/PendingQuotes` (`Pages/CommercialRoutine/PendingQuotes.cshtml`)

### Contracts

- **P1** — `/Contracts/Calendar` (`Pages/Contracts/Calendar.cshtml`)
- **P1** — `/Contracts/Create` (`Pages/Contracts/Create.cshtml`)
- **P1** — `/Contracts/Dashboard` (`Pages/Contracts/Dashboard.cshtml`)
- **P1** — `/Contracts/Details` (`Pages/Contracts/Details.cshtml`)
- **P1** — `/Contracts` (`Pages/Contracts/Index.cshtml`)

### Dashboard

- **P0** — `/Dashboard` (`Pages/Dashboard/Index.cshtml`)

### Documents

- **P1** — `/Documents/CreateBudget` (`Pages/Documents/CreateBudget.cshtml`)
- **P1** — `/Documents/CreateReceipt` (`Pages/Documents/CreateReceipt.cshtml`)
- **P1** — `/Documents/Details` (`Pages/Documents/Details.cshtml`)
- **P1** — `/Documents/Edit` (`Pages/Documents/Edit.cshtml`)
- **P1** — `/Documents` (`Pages/Documents/Index.cshtml`)
- **P1** — `/Documents/New` (`Pages/Documents/New.cshtml`)
- **P1** — `/Documents/Preview` (`Pages/Documents/Preview.cshtml`)
- **P1** — `/Documents/_DocumentTable` (`Pages/Documents/_DocumentTable.cshtml`)

### Error

- **P2** — `/Error/Status` (`Pages/Error/Status.cshtml`)

### Exports

- **P2** — `/Exports` (`Pages/Exports/Index.cshtml`)

### Feedback

- **P2** — `/Feedback` (`Pages/Feedback/Index.cshtml`)

### Files

- **P2** — `/Files` (`Pages/Files/Index.cshtml`)

### Help

- **P2** — `/Help/Article` (`Pages/Help/Article.cshtml`)
- **P2** — `/Help/GerarPdf` (`Pages/Help/GerarPdf.cshtml`)
- **P2** — `/Help/Historico` (`Pages/Help/Historico.cshtml`)
- **P2** — `/Help` (`Pages/Help/Index.cshtml`)
- **P2** — `/Help/Modelos` (`Pages/Help/Modelos.cshtml`)
- **P2** — `/Help/PlanoPro` (`Pages/Help/PlanoPro.cshtml`)
- **P2** — `/Help/PrimeiroOrcamento` (`Pages/Help/PrimeiroOrcamento.cshtml`)
- **P2** — `/Help/PrimeiroRecibo` (`Pages/Help/PrimeiroRecibo.cshtml`)

### Import

- **P2** — `/Import/History` (`Pages/Import/History.cshtml`)
- **P2** — `/Import` (`Pages/Import/Index.cshtml`)

### Marketplace

- **P1** — `/Marketplace/Details` (`Pages/Marketplace/Details.cshtml`)
- **P1** — `/Marketplace` (`Pages/Marketplace/Index.cshtml`)
- **P1** — `/Marketplace/Installations` (`Pages/Marketplace/Installations.cshtml`)

### MessageTemplates

- **P2** — `/MessageTemplates` (`Pages/MessageTemplates/Index.cshtml`)

### Notifications

- **P2** — `/Notifications` (`Pages/Notifications/Index.cshtml`)

### Onboarding

- **P2** — `/Onboarding/Budget` (`Pages/Onboarding/Budget.cshtml`)
- **P2** — `/Onboarding/Business` (`Pages/Onboarding/Business.cshtml`)
- **P2** — `/Onboarding/Client` (`Pages/Onboarding/Client.cshtml`)
- **P2** — `/Onboarding/DocumentIdentity` (`Pages/Onboarding/DocumentIdentity.cshtml`)
- **P2** — `/Onboarding/Done` (`Pages/Onboarding/Done.cshtml`)
- **P2** — `/Onboarding` (`Pages/Onboarding/Index.cshtml`)
- **P2** — `/Onboarding/Service` (`Pages/Onboarding/Service.cshtml`)
- **P2** — `/Onboarding/_Progress` (`Pages/Onboarding/_Progress.cshtml`)

### Payments

- **P1** — `/Payments/Details` (`Pages/Payments/Details.cshtml`)
- **P1** — `/Payments` (`Pages/Payments/Index.cshtml`)
- **P1** — `/Payments/Register` (`Pages/Payments/Register.cshtml`)

### Productivity

- **P2** — `/Productivity` (`Pages/Productivity/Index.cshtml`)

### Profile

- **P2** — `/Profile` (`Pages/Profile/Index.cshtml`)
- **P2** — `/Profile/Privacy` (`Pages/Profile/Privacy/Index.cshtml`)

### PublicQuotes

- **P2** — `/PublicQuotes/View` (`Pages/PublicQuotes/View.cshtml`)

### Receipts

- **P1** — `/Receipts/Create` (`Pages/Receipts/Create.cshtml`)
- **P1** — `/Receipts/Details` (`Pages/Receipts/Details.cshtml`)
- **P1** — `/Receipts` (`Pages/Receipts/Index.cshtml`)

### Receivables

- **P2** — `/Receivables` (`Pages/Receivables/Index.cshtml`)

### Recommendations

- **P2** — `/Recommendations` (`Pages/Recommendations/Index.cshtml`)

### Recursos

- **P2** — `/Recursos` (`Pages/Recursos/Index.cshtml`)
- **P2** — `/Recursos/Materiais` (`Pages/Recursos/Materiais.cshtml`)

### ReleaseNotes

- **P2** — `/ReleaseNotes` (`Pages/ReleaseNotes/Index.cshtml`)

### Reports

- **P2** — `/Reports/Clients` (`Pages/Reports/Clients.cshtml`)
- **P2** — `/Reports/CommercialFunnel` (`Pages/Reports/CommercialFunnel.cshtml`)
- **P2** — `/Reports/Executive` (`Pages/Reports/Executive.cshtml`)
- **P2** — `/Reports/Financial` (`Pages/Reports/Financial.cshtml`)
- **P2** — `/Reports` (`Pages/Reports/Index.cshtml`)
- **P2** — `/Reports/Operational` (`Pages/Reports/Operational.cshtml`)
- **P2** — `/Reports/Recurring` (`Pages/Reports/Recurring.cshtml`)
- **P2** — `/Reports/Services` (`Pages/Reports/Services.cshtml`)
- **P2** — `/Reports/_Report` (`Pages/Reports/_Report.cshtml`)

### Schedule

- **P2** — `/Schedule` (`Pages/Schedule/Index.cshtml`)

### Search

- **P2** — `/Search` (`Pages/Search/Index.cshtml`)
- **P2** — `/Search/Results` (`Pages/Search/Results.cshtml`)

### Segmentos

- **P2** — `/Segmentos` (`Pages/Segmentos/Index.cshtml`)

### Services

- **P1** — `/Services/Categories` (`Pages/Services/Categories.cshtml`)
- **P1** — `/Services/Create` (`Pages/Services/Create.cshtml`)
- **P1** — `/Services/Details` (`Pages/Services/Details.cshtml`)
- **P1** — `/Services/Edit` (`Pages/Services/Edit.cshtml`)
- **P1** — `/Services` (`Pages/Services/Index.cshtml`)
- **P1** — `/Services/_Form` (`Pages/Services/_Form.cshtml`)

### Settings

- **P1** — `/Settings/ApiKeys` (`Pages/Settings/ApiKeys/Index.cshtml`)
- **P1** — `/Settings/Audit` (`Pages/Settings/Audit.cshtml`)
- **P1** — `/Settings/Branding` (`Pages/Settings/Branding.cshtml`)
- **P1** — `/Settings/BusinessUnits` (`Pages/Settings/BusinessUnits/Index.cshtml`)
- **P1** — `/Settings/Commercial` (`Pages/Settings/Commercial.cshtml`)
- **P1** — `/Settings/Company` (`Pages/Settings/Company.cshtml`)
- **P1** — `/Settings/Documents` (`Pages/Settings/Documents.cshtml`)
- **P1** — `/Settings` (`Pages/Settings/Index.cshtml`)
- **P1** — `/Settings/IntegrationHealth` (`Pages/Settings/IntegrationHealth/Index.cshtml`)
- **P1** — `/Settings/Integrations` (`Pages/Settings/Integrations/Index.cshtml`)
- **P1** — `/Settings/Notifications` (`Pages/Settings/Notifications.cshtml`)
- **P1** — `/Settings/Payments` (`Pages/Settings/Payments.cshtml`)
- **P1** — `/Settings/Permissions` (`Pages/Settings/Permissions.cshtml`)
- **P1** — `/Settings/Privacy` (`Pages/Settings/Privacy/Index.cshtml`)
- **P1** — `/Settings/Security` (`Pages/Settings/Security.cshtml`)
- **P1** — `/Settings/Setup` (`Pages/Settings/Setup.cshtml`)
- **P1** — `/Settings/Support` (`Pages/Settings/Support.cshtml`)
- **P1** — `/Settings/Teams` (`Pages/Settings/Teams/Index.cshtml`)
- **P1** — `/Settings/Users` (`Pages/Settings/Users.cshtml`)
- **P1** — `/Settings/Webhooks` (`Pages/Settings/Webhooks/Index.cshtml`)
- **P1** — `/Settings/_SettingsNav` (`Pages/Settings/_SettingsNav.cshtml`)

### Shared

- **P2** — `/Shared/Partials/_ActionButton` (`Pages/Shared/Partials/_ActionButton.cshtml`)
- **P2** — `/Shared/Partials/_ActionMenu` (`Pages/Shared/Partials/_ActionMenu.cshtml`)
- **P2** — `/Shared/Partials/_AuthenticatedNavigation` (`Pages/Shared/Partials/_AuthenticatedNavigation.cshtml`)
- **P2** — `/Shared/Partials/_CalloutBox` (`Pages/Shared/Partials/_CalloutBox.cshtml`)
- **P2** — `/Shared/Partials/_ConfirmModal` (`Pages/Shared/Partials/_ConfirmModal.cshtml`)
- **P2** — `/Shared/Partials/_ContextGuide` (`Pages/Shared/Partials/_ContextGuide.cshtml`)
- **P2** — `/Shared/Partials/_EmptyState` (`Pages/Shared/Partials/_EmptyState.cshtml`)
- **P2** — `/Shared/Partials/_ExplainerCard` (`Pages/Shared/Partials/_ExplainerCard.cshtml`)
- **P2** — `/Shared/Partials/_FaqItem` (`Pages/Shared/Partials/_FaqItem.cshtml`)
- **P2** — `/Shared/Partials/_FeatureCard` (`Pages/Shared/Partials/_FeatureCard.cshtml`)
- **P2** — `/Shared/Partials/_FilterPanel` (`Pages/Shared/Partials/_FilterPanel.cshtml`)
- **P2** — `/Shared/Partials/_HelpShortcutCard` (`Pages/Shared/Partials/_HelpShortcutCard.cshtml`)
- **P2** — `/Shared/Partials/_Icon` (`Pages/Shared/Partials/_Icon.cshtml`)
- **P2** — `/Shared/Partials/_IconSprite` (`Pages/Shared/Partials/_IconSprite.cshtml`)
- **P2** — `/Shared/Partials/_MetricCard` (`Pages/Shared/Partials/_MetricCard.cshtml`)
- **P2** — `/Shared/Partials/_MetricStrip` (`Pages/Shared/Partials/_MetricStrip.cshtml`)
- **P2** — `/Shared/Partials/_MnsoftBrand` (`Pages/Shared/Partials/_MnsoftBrand.cshtml`)
- **P2** — `/Shared/Partials/_MnsoftFooter` (`Pages/Shared/Partials/_MnsoftFooter.cshtml`)
- **P2** — `/Shared/Partials/_NextAction` (`Pages/Shared/Partials/_NextAction.cshtml`)
- **P2** — `/Shared/Partials/_OverlayHost` (`Pages/Shared/Partials/_OverlayHost.cshtml`)
- **P2** — `/Shared/Partials/_PageHeader` (`Pages/Shared/Partials/_PageHeader.cshtml`)
- **P2** — `/Shared/Partials/_PageIntro` (`Pages/Shared/Partials/_PageIntro.cshtml`)
- **P2** — `/Shared/Partials/_PlanBadge` (`Pages/Shared/Partials/_PlanBadge.cshtml`)
- **P2** — `/Shared/Partials/_ProductBudgetPreview` (`Pages/Shared/Partials/_ProductBudgetPreview.cshtml`)
- **P2** — `/Shared/Partials/_ProductClient360Preview` (`Pages/Shared/Partials/_ProductClient360Preview.cshtml`)
- **P2** — `/Shared/Partials/_ProductDashboardPreview` (`Pages/Shared/Partials/_ProductDashboardPreview.cshtml`)
- **P2** — `/Shared/Partials/_ProductFlowPreview` (`Pages/Shared/Partials/_ProductFlowPreview.cshtml`)
- **P2** — `/Shared/Partials/_ProductPipelinePreview` (`Pages/Shared/Partials/_ProductPipelinePreview.cshtml`)
- **P2** — `/Shared/Partials/_ProductReceiptPreview` (`Pages/Shared/Partials/_ProductReceiptPreview.cshtml`)
- **P2** — `/Shared/Partials/_ProductServiceCatalogPreview` (`Pages/Shared/Partials/_ProductServiceCatalogPreview.cshtml`)
- **P2** — `/Shared/Partials/_ProfessionCard` (`Pages/Shared/Partials/_ProfessionCard.cshtml`)
- **P2** — `/Shared/Partials/_StatusBadge` (`Pages/Shared/Partials/_StatusBadge.cshtml`)
- **P2** — `/Shared/Partials/_StepCard` (`Pages/Shared/Partials/_StepCard.cshtml`)
- **P2** — `/Shared/Partials/_ToastHost` (`Pages/Shared/Partials/_ToastHost.cshtml`)
- **P2** — `/Shared/_AuthLayout` (`Pages/Shared/_AuthLayout.cshtml`)
- **P2** — `/Shared/_ClientLayout` (`Pages/Shared/_ClientLayout.cshtml`)
- **P2** — `/Shared/_ConfirmModal` (`Pages/Shared/_ConfirmModal.cshtml`)
- **P2** — `/Shared/_ConfirmationDialog` (`Pages/Shared/_ConfirmationDialog.cshtml`)
- **P2** — `/Shared/_DocumentTable` (`Pages/Shared/_DocumentTable.cshtml`)
- **P2** — `/Shared/_EmptyState` (`Pages/Shared/_EmptyState.cshtml`)
- **P2** — `/Shared/_FeatureDemoModal` (`Pages/Shared/_FeatureDemoModal.cshtml`)
- **P2** — `/Shared/_HelpDrawer` (`Pages/Shared/_HelpDrawer.cshtml`)
- **P2** — `/Shared/_InfoPopover` (`Pages/Shared/_InfoPopover.cshtml`)
- **P2** — `/Shared/_InfoTooltip` (`Pages/Shared/_InfoTooltip.cshtml`)
- **P2** — `/Shared/_InfrastructureUnavailable` (`Pages/Shared/_InfrastructureUnavailable.cshtml`)
- **P2** — `/Shared/_Layout` (`Pages/Shared/_Layout.cshtml`)
- **P2** — `/Shared/_MetricCard` (`Pages/Shared/_MetricCard.cshtml`)
- **P2** — `/Shared/_PageHeader` (`Pages/Shared/_PageHeader.cshtml`)
- **P2** — `/Shared/_PlanLimitDialog` (`Pages/Shared/_PlanLimitDialog.cshtml`)
- **P2** — `/Shared/_PlanUsageSummary` (`Pages/Shared/_PlanUsageSummary.cshtml`)
- **P2** — `/Shared/_PreservedDataDialog` (`Pages/Shared/_PreservedDataDialog.cshtml`)
- **P2** — `/Shared/_PublicLayout` (`Pages/Shared/_PublicLayout.cshtml`)
- **P2** — `/Shared/_PublicQuoteLayout` (`Pages/Shared/_PublicQuoteLayout.cshtml`)
- **P2** — `/Shared/_RegularizationDrawer` (`Pages/Shared/_RegularizationDrawer.cshtml`)
- **P2** — `/Shared/_StatusBadge` (`Pages/Shared/_StatusBadge.cshtml`)
- **P2** — `/Shared/_Toast` (`Pages/Shared/_Toast.cshtml`)

### Subscription

- **P2** — `/Subscription/BillingProfile` (`Pages/Subscription/BillingProfile.cshtml`)
- **P2** — `/Subscription` (`Pages/Subscription/Index.cshtml`)

### Support

- **P1** — `/Support` (`Pages/Support/Index.cshtml`)
- **P1** — `/Support/New` (`Pages/Support/New.cshtml`)
- **P1** — `/Support/TicketDetails` (`Pages/Support/TicketDetails.cshtml`)
- **P1** — `/Support/Tickets` (`Pages/Support/Tickets.cshtml`)

### Templates

- **P2** — `/Templates/Details` (`Pages/Templates/Details.cshtml`)
- **P2** — `/Templates` (`Pages/Templates/Index.cshtml`)

### Trust

- **P2** — `/Trust` (`Pages/Trust/Index.cshtml`)

### WorkOrders

- **P1** — `/WorkOrders/Details` (`Pages/WorkOrders/Details.cshtml`)
- **P1** — `/WorkOrders` (`Pages/WorkOrders/Index.cshtml`)

## Resultado e decisão

A fundação compartilhada é o ponto de correção prioritário: tokens, navegação, superfícies, tabelas, timelines, kanban, estados de feedback e regras mobile. A validação manual deve usar dados reais e permissões reais; esta auditoria não declara fluxos autenticados aprovados sem ambiente de banco.
