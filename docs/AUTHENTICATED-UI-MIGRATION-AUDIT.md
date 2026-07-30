# Authenticated UI migration audit

## Scope and gate

This inventory covers Razor views inheriting the authenticated client shell and every Admin Area view. Public, authentication, and public-quote layouts are deliberately excluded. The automated gate is `npm run check:authenticated-ui`; it scans Bootstrap classes and icons, `data-bs-*`, Bootstrap JavaScript/CDNs, jQuery, and blocked legacy modal partials.

**Baseline (2026-07-30):** 96 files audited; 57 files have 299 references under time-bound exception; 39 files are clean. Exceptions expire on 2026-09-30 and are tracked in issue #80. The release remains incomplete until this number reaches zero.

Visual and navigation columns below are intentionally marked pending: .NET and browser execution are unavailable in the current workspace, so no result is fabricated. CI must provide those results before this draft can be promoted.

## Page-by-page inventory

| File | Route/functionality | Legacy dependency | Current component/shell | Migration | Visual test | Navigation test |
|---|---|---|---|---|---|---|
| `src/OrcaFacil.Web/Areas/Admin/Pages/Accounts/Details.cshtml` | `/Admin/Accounts/Details` | none detected | Admin shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Clients/Index.cshtml` | `/Admin/Clients` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Dashboard.cshtml` | `/Admin/Dashboard` | none detected | Admin shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Errors.cshtml` | `/Admin/Errors` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Logs.cshtml` | `/Admin/Logs` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Payments/Details.cshtml` | `/Admin/Payments/Details` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Payments/Index.cshtml` | `/Admin/Payments` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Plans/Index.cshtml` | `/Admin/Plans` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Settings/Database.cshtml` | `/Admin/Settings/Database` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_EmptyState | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Settings.cshtml` | `/Admin/Settings` | none detected | Admin shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Shared/_AdminLayout.cshtml` | `partial/layout` | none detected | /Pages/Shared/Partials/_ToastHost.cshtml | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Users/Details.cshtml` | `/Admin/Users/Details` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Users/Index.cshtml` | `/Admin/Users` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/Users.cshtml` | `/Admin/Users` | legacy Bootstrap/UI tokens (temporary exception) | Admin shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/_ViewImports.cshtml` | `partial/layout` | none detected | Admin shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Areas/Admin/Pages/_ViewStart.cshtml` | `partial/layout` | none detected | Admin shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Admin/Index.cshtml` | `/Admin` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Ajuda.cshtml` | `/Ajuda` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Clients/Create.cshtml` | `/Clients/Create` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Clients/Details.cshtml` | `/Clients/Details` | legacy Bootstrap/UI tokens (temporary exception) | _ClientDocumentList | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Clients/Edit.cshtml` | `/Clients/Edit` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Clients/Index.cshtml` | `/Clients` | legacy Bootstrap/UI tokens (temporary exception) | _ClientActions | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Clients/_ClientActions.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Clients/_ClientDocumentList.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml` | `/Dashboard` | none detected | Partials/_EmptyState, _DocumentTable, _PlanUsageSummary | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Diagnostico.cshtml` | `/Diagnostico` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Discover.cshtml` | `/Discover` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Documents/CreateBudget.cshtml` | `/Documents/CreateBudget` | legacy Bootstrap/UI tokens (temporary exception) | _DocumentForm | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Documents/CreateReceipt.cshtml` | `/Documents/CreateReceipt` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_ExplainerCard, _DocumentForm | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Documents/Details.cshtml` | `/Documents/Details` | legacy Bootstrap/UI tokens (temporary exception) | _StatusBadge | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Documents/Edit.cshtml` | `/Documents/Edit` | none detected | _DocumentForm | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Documents/Index.cshtml` | `/Documents` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_EmptyState, _DocumentTable | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Documents/_DocumentTable.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Emitente.cshtml` | `/Emitente` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Help/GerarPdf.cshtml` | `/Help/GerarPdf` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_CalloutBox | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Help/Historico.cshtml` | `/Help/Historico` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_CalloutBox | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Help/Modelos.cshtml` | `/Help/Modelos` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Help/PlanoPro.cshtml` | `/Help/PlanoPro` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_CalloutBox | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Help/PrimeiroOrcamento.cshtml` | `/Help/PrimeiroOrcamento` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_CalloutBox | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Help/PrimeiroRecibo.cshtml` | `/Help/PrimeiroRecibo` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_CalloutBox | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Historico.cshtml` | `/Historico` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Notifications/Index.cshtml` | `/Notifications` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Onboarding/Index.cshtml` | `/Onboarding` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_StepCard | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Payments/Register.cshtml` | `/Payments/Register` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Profile/Index.cshtml` | `/Profile` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Receipts/Details.cshtml` | `/Receipts/Details` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Schedule/Index.cshtml` | `/Schedule` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Services/Index.cshtml` | `/Services` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_EmptyState | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_ActionButton.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_CalloutBox.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_ConfirmModal.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_EmptyState.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_ExplainerCard.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_FaqItem.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_FeatureCard.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_HelpShortcutCard.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_MetricCard.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_MnsoftBrand.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_MnsoftFooter.cshtml` | `partial/layout` | none detected | Partials/_MnsoftBrand | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_PageHeader.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_PlanBadge.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_ProfessionCard.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_StatusBadge.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_StepCard.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/Partials/_ToastHost.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_AuthLayout.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_ToastHost | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_ClientLayout.cshtml` | `partial/layout` | none detected | Partials/_ToastHost | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_ConfirmModal.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_ConfirmationDialog.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_DocumentForm.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_DocumentTable.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | _StatusBadge | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_EmptyState.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_FeatureDemoModal.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_HelpDrawer.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_InfoPopover.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_InfoTooltip.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_InfrastructureUnavailable.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_ToastHost | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_MetricCard.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_PageHeader.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_PlanLimitDialog.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_PlanUsageSummary.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_PreservedDataDialog.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_PublicLayout.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Partials/_MnsoftBrand, Partials/_ToastHost | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_PublicQuoteLayout.cshtml` | `partial/layout` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_RegularizationDrawer.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_StatusBadge.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Shared/_Toast.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Subscription/BillingProfile.cshtml` | `/Subscription/BillingProfile` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Subscription/Index.cshtml` | `/Subscription` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/Templates/Details.cshtml` | `/Templates/Details` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/Templates/Index.cshtml` | `/Templates` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/WorkOrders/Details.cshtml` | `/WorkOrders/Details` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/WorkOrders/Index.cshtml` | `/WorkOrders` | legacy Bootstrap/UI tokens (temporary exception) | Client shell | pending migration | pending | pending |
| `src/OrcaFacil.Web/Pages/_ViewImports.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |
| `src/OrcaFacil.Web/Pages/_ViewStart.cshtml` | `partial/layout` | none detected | Client shell | guarded/clean | pending | pending |

## Exception policy

An exception must include the exact file, reason, owner, deadline, and related issue. Expired or incomplete entries fail the gate. New unlisted references fail immediately. Exceptions are migration debt, not permanent compatibility.

## Exit criteria

1. Remove every entry from `scripts/authenticated-ui-legacy-exceptions.json`.
2. Run the gate with zero legacy references.
3. Exercise every menu route and record its title, breadcrumb, active item, contextual help, authorization response, and HTTP status.
4. Capture non-versioned Playwright artifacts at all required viewports and pass axe WCAG AA checks.
