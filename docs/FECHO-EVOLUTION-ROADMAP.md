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
| R2 | **P1 — mitigado nesta execução (PostgreSQL)** | Testes de integração PostgreSQL estavam ignorados sem `ORCAFACIL_JOURNEY_CONNECTION`. Com banco **descartável** local (schema `orcafacil` restaurado por `pg_dump --schema-only` em `orcafacil_journey_20261016`), os 13 testes antes ignorados executaram: **683 aprovados, 0 falhas, 0 ignorados**. Restante do R2: evidência de navegador via Playwright — **Node v24 está presente**, mas `@playwright/test` **não está instalado** no ambiente e há 5 specs em `tests/playwright/*`; habilitar requer instalar a dependência, um servidor Web rodando e dados de seed (fora do repositório). | `JourneyPostgresTests.cs:19-21`, `HomologationPostgresTests.cs`, `tests/playwright/*` |
| R3 | **P1 — resolvido nesta execução** | Onboarding não tinha teste dedicado. As regras de etapa foram extraídas para `OnboardingStepRules` (puro) e cobertas por `OnboardingRulesTests`, incluindo a garantia de upsert idempotente `(account_id, user_id)`. | `OnboardingStepRules.cs`, `OnboardingRulesTests.cs` |
| R4 | **P0 (nova)** | Divergências históricas: **diagnóstico e revisão auditada** entregues nesta execução; a **correção** permanece manual e intencionalmente não automatizada (itens 4.3 e 20 proíbem correção em massa sem revisão). | `FinancialIntegrityDiagnostics.cs`, `SuperAdmin/Finance/Divergences` |
| R5 | **P0** | Gateway Mercado Pago sem validação sandbox real (assinatura de webhook, dedup, reconciliação). | `docs/KNOWN_ISSUES.md` |
| R6 | **P2** | Rota pública documentada como `/p/orcamento/{token}`; a implementação real é `/p/{token}`. Divergência de documentação. | `Pages/PublicQuotes/View.cshtml` |
| R7 | **P1 (novo — exposto apenas com PostgreSQL real)** | `manual_payments` possui **6 FKs** no banco (criadas por SQL cru em `AddManualPaymentsAndReceipts`) mas **nenhuma relação mapeada no modelo EF** (`ManualPaymentConfiguration` não declara `HasOne`). Sem o relacionamento o EF não ordena os `INSERT` por dependência e pode violar `manual_payments_work_order_id_fkey` (SQLSTATE 23503) ao gravar pagamento e ordem no mesmo `SaveChanges`. Além disso, a migration histórica declara `work_order_id uuid NOT NULL` enquanto o banco atual é **nullable** (a entidade é `Guid?`). Correção proposta (requer autorização — altera o modelo EF e exige sincronizar `OrcaFacilDbContextModelSnapshot`): declarar os 6 relacionamentos e refletir no snapshot, sem nova migration de schema, pois as constraints já existem. | `Configurations/CommercialJourneyConfiguration.cs:89-101`, `Migrations/20260730000000_AddManualPaymentsAndReceipts.cs:12`, `JourneyPostgresTests.cs` |
| R8 | **P2 (baseline pré-existente de validadores)** | Dos 611 scripts `check:*`, um grupo (~30–40) falha **também no commit-base `1386356`**, ou seja, não foi causado por esta branch. Temas principais: página de Preços sem comunicar “contratação online ainda não está habilitada” (~10 checks de billing/trial/quota/upgrade), CTA “Ver Profissional” ausente na Home, contrato legado `dashboard.BudgetTotal`, tokens do Design System V5.6, segredo real em `appsettings.json` (`appsettings-secrets`), menu mobile que não fecha por teclado/toque externo, string “OrçaFácil” em `InternalAssistantService.cs` (`assistant-safety`), fallback inválido apontado em `GoLiveV65Services.cs` (`dapper-mapping`/`source-code-audit`/`method-consistency`/`no-fake-flows`) e 3 serviços sem registro de DI (`di-consistency`: `FunctionalQualityService`, `ModuleReadinessService`, `SourceCodeFindingService`). Item de higienização independente desta evolução. | `package.json` (scripts `check:*`), `scripts/sprint41-check-core.mjs`, `scripts/sprint55-design-checks.mjs` |

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
- **Bloqueado:** evidência Playwright/navegador visível. O PostgreSQL local foi provido nesta execução (banco descartável) e os testes de integração passaram — ver §5 e R7.
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
- `dotnet test OrcaFacil.sln -c Release` (sem banco) → **670 aprovados, 0 falhas, 13 ignorados** (gate de PostgreSQL).
- `dotnet test OrcaFacil.sln -c Release` com `ORCAFACIL_JOURNEY_CONNECTION` apontando para banco **descartável**
  `orcafacil_journey_20261016` (schema `orcafacil` restaurado por `pg_dump --schema-only`; extensões `pgcrypto` e
  `pg_trgm`) → **683 aprovados, 0 falhas, 0 ignorados**. Inclui a jornada comercial completa, os 3 testes de
  homologação concorrente de IA e os cálculos de saldo/estorno.
- Defeito real encontrado **apenas** com PostgreSQL real e documentado em R7: ordem de `INSERT` de
  `manual_payments` (FK não mapeada no EF). O fixture de teste foi ajustado para persistir pais antes dos
  dependentes, mantendo o foco do teste na regra de saldo.
- **Validadores `package.json` (`check:*`, 611 scripts):** varridos contra o commit-base `1386356`.
  Os checks críticos para esta branch passam: `check:js`, `check:service-lifetimes`, `check:di-quality-gate`,
  `check:aspnet-scope`, `check:database-schema-contract`, `check:quality-gate-no-fake`, `check:build-blockers`,
  `check:csharp-test-types`, `check:query-safety`, `check:dapper-mapping`* e `check:financial-flow(-consistency|journey)`.
  As mudanças desta branch **não introduzem falha nova** (diferença vs base verificada).
  - *Correção aplicada:* os validadores `sprint41`/`sprint55` marcavam minha nova tela `Divergences.cshtml` como
    “ID técnico exposto como entrada livre” (o filtro era um `<input>` de GUID). Substituído por `<select>` de
    contas por nome → 8 checks (`functional-flows`, `portal-security`, `business-rules`, `commerce-flow`,
    `omnichannel-flow`, `support-flow`, `workflow-rules`, `bi-reports`) voltaram a passar.
  - `check:git-safety` só falhava por churn de artefatos `bin/obj` rastreados do build Release; restaurados ao
    HEAD fica verde. **Não** é defeito de código.
  - *Falhas pré-existentes (presentes também na base, fora do escopo desta branch):* mensagens da página de
    Preços (“contratação online ainda não está habilitada”), CTA “Ver Profissional” na Home, contrato legado
    `dashboard.BudgetTotal`, tokens do Design System V5.6, `appsettings-secrets`, fechamento do menu mobile,
    string `OrçaFácil` em `assistant-safety`, fallback inválido em `GoLiveV65Services.cs` e 3 serviços sem DI
    (`FunctionalQualityService`, `ModuleReadinessService`, `SourceCodeFindingService`). Detalhe completo em R8.
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
  `tests/OrcaFacil.UnitTests/QualityGateDependencyInjectionTests.cs`,
  `tests/OrcaFacil.UnitTests/JourneyPostgresTests.cs`.

## 6. Próxima ação

1. Autorizar a correção do R7 (relacionamentos `manual_payments` no modelo EF + sincronizar o snapshot), com revisão.
2. Fechar a evidência de navegador (R2): instalar `@playwright/test`, subir o servidor Web com seed e rodar as 5 specs de `tests/playwright/*`.
3. Validar gateway Mercado Pago em sandbox (R5) e o fluxo de inadimplência/regularização.
4. Evoluir a Sprint E (IA assistida) sobre a base de DI já corrigida.
5. Higienizar o baseline de validadores pré-existente (R8) — separadamente desta evolução.
6. Padronizar a rota pública documentada (R6).
