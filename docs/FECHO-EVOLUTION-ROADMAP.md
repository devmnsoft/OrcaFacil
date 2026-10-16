# Fechô 2.0 — Roadmap de evolução

**Repositório:** `github.com/devmnsoft/OrcaFacil` · **Identidade técnica:** OrcaFacil · **Marca comercial:** Fechô
**Branch de evolução:** `fecho/evolution-2.0` (derivada de `rebranding/fecho`, que traz o rebranding comercial já commitado)
**Data desta execução:** 16/10/2026 · **SDK:** .NET 10.0.400

> Este documento é o registro de progresso pedido no item 24. Ele separa **código existente**, **validação executada** e **resultado observado**. Nenhum item é marcado como homologado apenas por existir uma tela.

---

## 1. Método de auditoria

A auditoria partiu dos documentos históricos (`quote-to-cash-v6.28.0-audit.md`, `COMMERCIAL-JOURNEY-AUDIT.md`,
`EXPERIENCE-SYSTEM-IMPLEMENTATION-AUDIT.md`, `KNOWN_ISSUES.md`, `GO-LIVE-V1.md`, `PLAN-FEATURES.md`,
`HOMOLOGACAO-FINAL-V1.md`) **apenas como roteiro de verificação**, e confirmou o estado diretamente em:
entidades de domínio, `DbSet`s do `OrcaFacilDbContext`, serviços de Application/Persistence, PageModels Razor,
registros de DI em `Program.cs` e testes automatizados.

Status usados: **Funcional** (backend + persistência + interface + regra + testes), **Parcial**, **Ausente**, **Não validado**.

---

## 2. Matriz de auditoria (verificada no código)

| Módulo | Backend | Banco | Interface | Regra de negócio | Testes | Status |
|---|---|---|---|---|---|---|
| Onboarding guiado | `OnboardingApplicationService`, `OnboardingContracts` | `AccountOnboardingState` (+ migration) | `Pages/Onboarding/*` | progresso por etapa, upsert `(account_id,user_id)`, save/resume | sem teste dedicado | **Funcional** (teste ausente) |
| Dashboard orientado à ação | `DashboardExperienceService`, `NextBestActionService`, `CommercialNextActionResolver` | `DashboardQueries` (Dapper real) | `Pages/Dashboard/Index` | métricas reais; "próximo passo" a partir de dados | `DashboardDesignModelTests`, `DashboardDbContextSafetyTests` | **Funcional** |
| Clientes (mini-CRM) | `ClientWorkspaceService` | `clients` | `Pages/Clients/{Index,Create,Edit,Details}` | normalização, anti-duplicidade, soft delete, timeline | `ClientPageTests` | **Funcional** |
| Catálogo de serviços | `ServiceCatalogApplicationService` | `ServiceCatalogItems`, `ServicePriceHistory` | `Pages/Services/*` | CRUD real, histórico de preço | testes de contrato | **Funcional** (não é mais stub) |
| Orçamento (wizard) | `BudgetWizardService`, `GuidedBudgetStartService` | `documents`, `document_items` | `Pages/Documents/CreateBudget` | multi-etapa, autosave, rascunho idempotente | `BudgetWizard*` | **Funcional** |
| Máquina de estados do documento | `DocumentStatusTransitionService` | `documents.status` | — | transições explícitas, enforcement no servidor | `CommercialStatusTransitionTests` | **Funcional** |
| Aprovação pública | `PublicDocumentAccessService`, `PublicDocumentTokenService` | `public_document_access/decisions` (hash SHA-256) | `/p/{token}` (`Pages/PublicQuotes/View`) | hash, expiração/revogação, idempotência, concorrência | `PublicDocumentTokenServiceTests` | **Funcional** (rota difere de `/p/orcamento/{token}` citada em doc) |
| Ordem de serviço | `CommercialJourneyService` (conversão), `WorkOrderStatusTransitionService` | `work_orders`, `work_order_checklist_items` | `Pages/WorkOrders/*` | conversão idempotente, estados, checklist, agenda | testes de transição | **Funcional** |
| Recebíveis / pagamentos / recibos | `ReceiptApplicationService`, `CommercialJourneyService`, `CommercialBalanceService` | `manual_payments`, `receipts` | `Pages/Receivables`, `Pages/Payments`, `Pages/Receipts` | idempotência, parcial/integral, estorno, recibo por pagamento | `PaymentSafetyTests`, `CommercialBalanceCalculatorTests` | **Funcional** |
| Acompanhamento comercial | `CommercialJourneyService` (follow-up) | `commercial_follow_ups`, `documents.next_follow_up_at` | `Pages/CommercialRoutine`, detalhes | agendar/adiar/concluir | parcial | **Funcional** |
| **Integridade financeira histórica (P0 4.3)** | **`FinancialIntegrityDiagnosticService` + `FinancialIntegrityAnalyzer`** | leitura de `manual_payments`, `receipts`, `work_orders`, `documents` | **`/SuperAdmin/Finance/Divergences`** | 9 classificações de divergência, risco e ação recomendada; revisão auditada | **`FinancialIntegrityDiagnosticsTests` (16)** | **Funcional (novo nesta execução)** |
| **Filtro financeiro min/max (P0 4.4)** | `QuoteWorkspaceService` | pushdown SQL de `document.Total` | — | pushdown quando não há revisão/OS; fallback em memória preservado | **`QuoteWorkspaceFilterPushdownTests`** | **Funcional (novo nesta execução)** |
| Planos e assinaturas | `PlanAccessService`, `EfPlanAccessDataSource`, `EfPlanCatalogService` | `plans`, `plan_versions`, `plan_features`, `account_module_subscriptions` | `Pages/Subscription`, `Areas/Admin/Plans` | catálogo versionado, limites persistidos, sem `if` por nome | `PlanAccessServiceTests` | **Funcional** |
| SuperAdministrador | Pages em `Pages/SuperAdmin` | várias | shell global + menu dinâmico | policy `SuperAdminOnly`, auditoria de ações críticas | `SuperAdminGlobalAccessTests`, `SaasEnterpriseV66Tests` | **Funcional** |
| Assistente de IA | `AiOrchestrator`, `AiClients` (Groq/Gemini/DeepSeek) | `AiOperations` | `Pages/Assistant` | orquestração + governança; DI corrigida (scoped) | contratos de DI + governança | **Parcial** (nestes termos: provedores reais dependem de credenciais) |
| Integração de cobrança (Mercado Pago) | `Infrastructure/Payments` | `payment_events`, `billing_invoices` | retorno/webhook | dedup/reprocessamento | testes de contrato | **Parcial** (sandbox não validado) |

---

## 3. Riscos e lacunas encontrados na verificação

Classificação de pendências: **P0** impede produção · **P1** impede operação adequada · **P2** melhoria posterior.

| ID | Severidade | Descrição | Evidência |
|---|---|---|---|
| R1 | **P1 — resolvido nesta execução** | Clientes de IA (`GroqAiClient`, `GeminiAiClient`, `DeepSeekAiClient`) eram registrados como `Singleton` mas obtinham o `HttpClient` do factory, capturando um cliente de longa duração. Agora são `AddScoped`, alinhados ao `AiOrchestrator` (também scoped); o `HttpClient` é recriado por requisição via `IHttpClientFactory`. | `Web/Program.cs:213-227`, `Infrastructure/Ai/AiClients.cs` |
| R2 | **P0 (bloqueio de evidência)** | Testes de integração PostgreSQL estão ignorados sem `ORCAFACIL_JOURNEY_CONNECTION`; Playwright não executado. Sem ambiente, a jornada ponta a ponta **não é homologada**. | `JourneyPostgresTests.cs:19-21`, `tests/playwright/*` |
| R3 | **P1 — resolvido nesta execução** | Onboarding não tinha teste dedicado. As regras de etapa foram extraídas para `OnboardingStepRules` (puro) e cobertas por `OnboardingRulesTests`, incluindo a garantia de upsert idempotente `(account_id, user_id)`. | `OnboardingStepRules.cs`, `OnboardingRulesTests.cs` |
| R4 | **P0 (nova)** | Divergências históricas: **diagnóstico e revisão auditada** entregues nesta execução; a **correção** permanece manual e intencionalmente não automatizada (itens 4.3 e 20 proíbem correção em massa sem revisão). | `FinancialIntegrityDiagnostics.cs`, `SuperAdmin/Finance/Divergences` |
| R5 | **P0** | Gateway Mercado Pago sem validação sandbox real (assinatura de webhook, dedup, reconciliação). | `docs/KNOWN_ISSUES.md` |
| R6 | **P2** | Rota pública documentada como `/p/orcamento/{token}`; a implementação real é `/p/{token}`. Divergência de documentação. | `Pages/PublicQuotes/View.cshtml` |

---

## 4. Sprints

### Sprint A — Integridade e fundação (P0) — **em andamento**

- **Objetivo:** fluxo financeiro consistente, rastreável e protegido.
- **Dependências:** `OrcaFacil.sln`, PostgreSQL para integração.
- **Implementado nesta execução:**
  - Diagnóstico não destrutivo de divergências financeiras históricas (item 4.3): analisador puro + serviço de leitura + tela restrita `/SuperAdmin/Finance/Divergences` com revisão auditada (`AccountModuleAuditLog`) e entrada no menu global.
  - Pushdown SQL dos filtros financeiros min/max e ordenação por valor quando a conta não possui revisões nem ordens (item 4.4), preservando o caminho em memória para contratos versionados.
  - 19 testes automatizados novos (16 do diagnóstico + 3 do pushdown).
  - Correção de um contrato de teste obsoleto pré-existente que referenciava `dashboard.BudgetTotal` (métrica existe, mas a tela redesenhada passou a exibir `TotalBudgets`/`TotalDocuments`).
- **Já existente e verificado:** seleção determinística de origem contratual (`CommercialContractOriginSelector`), imutabilidade de revisão, classificação de vínculos (`CommercialPaymentClassifier`), bloqueio de novo recebimento sob divergência impeditiva.
- **Bloqueado:** backfill/report em SQL executado contra dados reais e evidência Playwright — requer PostgreSQL e navegador visível (R2).
- **Critérios de aceite:** build Release sem erros; suíte verde exceto itens explicitamente bloqueados; toda correção autorizada gera auditoria; nenhuma correção em massa automática.

### Sprint B — Jornada comercial (P0) — **não iniciado nesta execução**

Já funcional conforme matriz (onboarding → orçamento → aprovação → OS → recebíveis → recibo). Pendências:
teste dedicado de onboarding (R3), evidência ponta a ponta (R2), conferência de acessibilidade mobile (320–1440 px).

### Sprint C — Monetização (P0) — **não iniciado nesta execução**

Catálogo versionado e limites persistidos existem. Pendente: validação de gateway em sandbox (R5), fluxo de
inadimplência/regularização ponta a ponta e administração global de assinaturas com evidência.

### Sprint D — Design Fechô 2.0 (P1) — **parcial**

Rebranding comercial concluído (commit `1386356`). Ajustes finos de navegação/responsividade ainda dependem de
evidência Playwright (R2).

### Sprint E — Inteligência e produtividade (P1) — **parcial**

Orquestrador e provedores existem. **Vida útil de DI dos clientes de IA corrigida** (R1, registrados como
`Scoped`, com teste de contrato). Próximo passo: evoluir orçamento assistido, sugestão de textos e
acompanhamento comercial com cobertura de testes.

### Sprint F — Homologação final (P0) — **não iniciado**

Depende de PostgreSQL descartável, HTTPS/SMTP, navegador visível e credenciais fora do repositório.

---

## 5. Evidências desta execução

- `dotnet build OrcaFacil.sln -c Release` → **0 erros**.
- `dotnet test OrcaFacil.sln -c Release` → ver resultado consolidado na resposta da execução.
- Arquivos criados: `src/OrcaFacil.Application/Commercial/FinancialIntegrityDiagnostics.cs`,
  `src/OrcaFacil.Persistence/Services/FinancialIntegrityDiagnosticService.cs`,
  `src/OrcaFacil.Web/Pages/SuperAdmin/Finance/Divergences.cshtml(.cs)`,
  `src/OrcaFacil.Application/Onboarding/OnboardingStepRules.cs`,
  `tests/OrcaFacil.UnitTests/FinancialIntegrityDiagnosticsTests.cs`,
  `tests/OrcaFacil.UnitTests/QuoteWorkspaceFilterPushdownTests.cs`,
  `tests/OrcaFacil.UnitTests/OnboardingRulesTests.cs`, este documento.
- Arquivos alterados: `src/OrcaFacil.Web/Program.cs`, `src/OrcaFacil.Api/Program.cs`,
  `src/OrcaFacil.Web/Services/ModuleMenuComposer.cs`, `src/OrcaFacil.Persistence/Services/QuoteWorkspaceService.cs`,
  `src/OrcaFacil.Persistence/Services/OnboardingApplicationService.cs`,
  `tests/OrcaFacil.UnitTests/Sprint55PremiumDesignTests.cs`,
  `tests/OrcaFacil.UnitTests/QualityGateDependencyInjectionTests.cs`.

## 6. Próxima ação

1. Disponibilizar PostgreSQL descartável e navegador para habilitar `JourneyPostgresTests` e Playwright (R2).
2. Validar gateway Mercado Pago em sandbox (R5) e o fluxo de inadimplência/regularização.
3. Evoluir a Sprint E (IA assistida) sobre a base de DI já corrigida.
4. Padronizar a rota pública documentada (R6).
