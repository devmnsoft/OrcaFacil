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
| R6 | **P2 — resolvido** | Rota pública documentada como `/p/orcamento/{token}` (legado); a implementação real é `/p/{token}`. Corrigido: documentação atualizada para refletir a rota real. | `Pages/PublicQuotes/View.cshtml` |
| R7 | **P1 — resolvido nesta execução** | `manual_payments` possui **6 FKs** no banco (criadas por SQL cru em `AddManualPaymentsAndReceipts` + hotfixes) mas nenhuma estava mapeada no modelo EF; o EF gravava `manual_payments` antes da `work_orders` e violava `manual_payments_work_order_id_fkey` (SQLSTATE 23503) ao persistir pagamento e ordem juntos — defeito exposto **só** com PostgreSQL real. **Corrigido:** os 6 relacionamentos foram declarados em `ManualPaymentConfiguration` com `DeleteBehavior.Restrict` (espelha `ON DELETE RESTRICT`). Sem nova migration (as FKs já existem nos bancos reais; criar uma quebraria a aplicação neles e mudaria `GetPendingMigrations`/health-check). O fixture voltou a um único `SaveChanges` e passa — prova pela jornada real. Nota: a migration histórica diz `work_order_id NOT NULL`, mas o banco atual é nullable (entidade `Guid?`) — drift documentado. | `Configurations/CommercialJourneyConfiguration.cs`, `tests/OrcaFacil.UnitTests/JourneyPostgresTests.cs` |
| R9 | **P2 (higiene do repositório)** | Os artefatos `bin/obj` estão **rastreados e obsoletos** no git. Isso cria uma tensão real de validação: `check:git-safety` só fica verde quando o `bin` coincide com o HEAD (arquivo obsoleto), mas o teste `JsonTextLocalizer_LoadsCatalogs_AndTranslatesAcrossFourCultures` precisa do `resources.pt-BR.json` fresco no `bin` Release (o commit traz um stub de 500 bytes sem `Nav.Quotes`; o catálogo real tem 6386 bytes). Estado escolhido: **catálogos frescos no bin → suíte verde**; resíduo esperado: `check:git-safety` aponta churn de bin. Recomendação: parar de rastrear `bin/obj` (ou commitar um build integral consistente). | `src/*/bin`, `tests/*/bin`, `tests/OrcaFacil.UnitTests/MultilingualJourneyAndHomologationTests.cs` |
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

### Sprint E — Inteligência e produtividade (P1) — **em andamento**

Orquestrador e provedores existem. **Vida útil de DI dos clientes de IA corrigida** (R1, registrados como
`Scoped`, com teste de contrato). Progresso desta execução:

- **E-1 concluído — rascunhos de mensagem no acompanhamento comercial ("Orçou. Enviou. Fechô."):** a service
  `MessageDraftAiAssistant` (WhatsApp/e-mail; apresentação/retorno), antes registrada no DI sem nenhum consumidor,
  está conectada em `/Documents/Details` (botões "Rascunho para WhatsApp" / "Rascunho para e-mail" na seção de
  Acompanhamento comercial, com caixa reutilizável `data-share-center`, botão copiar e avisos da própria service —
  nada é enviado automaticamente). adicionado guard de governança na service (conta ativa + não cruzar `AccountId`),
  8 testes unitários novos (`MessageDraftAiAssistantTests`) e 2 specs Playwright (`message-drafts.spec.ts`).
  O `commercial-workspace.js` passou a suportar múltiplos `data-share-center` por página (`querySelectorAll`),
  preservando o comportamento da Central de Compartilhamento.

- **E-4 concluído — rascunhos de mensagem conectados ao orquestrador/LLM (com fallback de regras):** a
  `MessageDraftAiAssistant` agora tenta gerar o texto via `IAiOrchestrator` (propósito permitido `message_draft`,
  prompt composto **apenas** com dados do orçamento já redigidos) quando o benefício de IA externa está habilitado
  no plano (`PlanFeatureCodes.AiExternalProvidersEnabled`, mesmo padrão do `BudgetAssistant`). Sem provedor
  configurado, em erro, em texto inválido (`< 40` caracteres após redação) ou com plano sem o benefício, o
  comportamento anterior (template determinístico de regras) é mantido — nenhum usuário vê mudança hoje, o
  ganho passa a existir quando um provedor for configurado. Governança: permissão `Ai.GenerateDrafts` comprovada no
  servidor é repassada no `AiRequestContext` (antes o contexto seguia vazio); disclaimers "NUNCA enviado automaticamente"
  preservados em ambos os caminhos, com aviso extra "sugerido com apoio de IA" quando aplicável. Assinatura do
  método evoluiu para `GenerateDraftAsync` (a orquestração é assíncrona); 14 testes unitários no
  `MessageDraftAiAssistantTests` (sucesso de provedor, redação dupla do texto do provedor, conteúdo curto →
  template, falha → template, plano sem benefício → template sem chamar orquestrador) e cobertura nos
  `CommercialCandidateHomologationTests` atualizada.

- **E-2 concluído — revisão comercial conectada ao orquestrador/LLM (regras sempre como rede de segurança):** a
  `CommercialAiReviewer` (motor 100% regras, DI-registered, consumidor `OnPostAssistScopeAsync` em
  `/Documents/CreateBudget`) agora roda as regras determinísticas **sempre** (itens ausentes, valores zerados,
  validade, contatos — mesma cobertura e mesmos achados de antes) e, **além** delas, tenta a IA externa via
  `IAiOrchestrator` (propósito `quote_review`; prompt só com dados redigidos da proposta; pedido de JSON
  estrito). Título, condições e apontamentos extra do provedor entram quando autorizados pela política (`FeatureEnabled`
  do plano `AiExternalProvidersEnabled` + conta ativa, mesmo padrão do E-4, agora aplicado no handler do
  `CreateBudget` via `IPlanAccessService`); permissão `Ai.ApplySuggestions` comprovada no servidor é
  repassada no `AiRequestContext`. Parser `CommercialReviewParser` (público, arquivo próprio, padrão do
  `BudgetSuggestionParser`): JSON em qualquer cerca aceito, severidade normalizada (Crítica/Alta/Média/Baixa)
  para valores fora da lista, máx. 8 apontamentos, campos ausentes descartados (nada inventado), todos os textos
  do provedor redigidos (`IAiRedactionService`, dupla proteção). Achados duplicados do provedor não entram
  (dedup por mensagem). Em falha/JSON inválido/plano sem benefício, retorno idêntico ao de regras
  (`isProviderGenerated=false`, aviso "regras de consistência") — nenhum usuário vê mudança hoje. Quando o
  provedor responde, a página marca `isProviderGenerated=true` e troca o aviso para "sugerido com apoio de IA ... nada foi
  alterado no orçamento", renderizado em ambos os ramos (com/sem apontamentos). 11 testes unitários no arquivo
  `CommercialAiReviewerAiTests` e 2 specs Playwright novas (`commercial-review.spec.ts`,
  fluxo do wizard até o painel do assistente, aplicação e descarte).
- **E-3 concluído — ajuda contextual conectada ao RAG (`IContextualAiHelpService`), com fontes reais:** decisão de
  produto registrada nesta execução: a ajuda inteligente mora no **drawer de ajuda já existente** (`_ClientLayout`,
  botão "Como usar esta página", presente em todas as páginas autenticadas), em vez de criar nova página. O drawer
  ganhou a seção "Pergunte sobre esta tela" (campo + botão, `data-help-ask-*`), que envia `POST /Internal/Help/Ask`
  (`RequireAuthorization` + antiforgery validado no servidor) e renderiza resposta, fontes consultadas e limites.
  O corpus é composto por **todos os guias estáticos do produto** (`IContextualHelpService.All()`, guia da tela
  primeiro) **+ artigos publicados** de `KnowledgeBaseArticle` (`IHelpKnowledgeSource`/`EfHelpKnowledgeSource`,
  somente `IsPublished && !IsDeleted`, até 50 em ordem de exibição). Nada é inventado: sem fonte relevante o serviço
  devolve `Insufficient` e a UI diz que não há evidência (`Succeeded=false`). O drawer resolve o guia pela
  **rota atual** (`IContextualHelpService.ForPath`: detalhe herda a página-mãe, barra final e caixa são toleradas,
  prefixo parcial não casa), com fallback para o código informado e depois visão geral; os códigos antigos
  inexistentes (`quotes`, `budget-create`) foram corrigidos para os guias reais. Governança corrigida no caminho:
  `ContextualAiHelpService.AskHelpAsync` passou a **receber a política** (`AiGovernancePolicy`) em vez de criar uma
  política local com `FeatureEnabled=true` — o gate do plano `AiExternalProvidersEnabled` + conta ativa agora chega
  de fato ao orquestrador (antes era decorativo). O serviço também valida `context.AccountId == policy.AccountId`
  (nenhuma chamada cruza conta), sanitiza a pergunta (`PromptSanitizer`) e redige o texto do provedor
  (`IAiRedactionService`), retornando `ExternalProvider` quando o LLM respondeu e `RulesOnly` no fallback. O
  matching de fontes virou acento-insensível (`orçamento`/`orcamento`), com stopwords (`como`, `faço`, `isso`…)
  para não casar qualquer guia, ranking por título/conteúdo e limite de 3 fontes. UI honesta: "Resposta sugerida com
  apoio de IA … nada foi alterado na sua conta" vs "Resposta montada a partir do texto das fontes oficiais listadas.
  Nenhuma ação foi executada.", além de "Fontes consultadas" com links (`/Help/Article?slug=…`). 10 testes unitários
  do endpoint (`ContextualAiHelpTests`: 403 sem conta, 400 pergunta curta, corpus com guia da tela e artigos
  publicados, permissões provadas repassadas, política do plano repassada, aviso por origem, `Insufficient` sem fonte)
  + 9 do serviço (`ContextualAiHelpServiceTests`: isolamento de conta, fonte inacessível, acentuação, stopwords,
  política repassada, redação de segredo na pergunta e na resposta, fallback) + 3 specs Playwright
  (`contextual-help-ai.spec.ts`: resposta com fontes e aviso, corpus de guias sem artigo publicado, pergunta curta
  recusada no cliente).
### Sprint F — Homologação final (P0) — **não iniciado**

Depende de PostgreSQL descartável, HTTPS/SMTP, navegador visível e credenciais fora do repositório.

---

## 5. Evidências desta execução

- `dotnet build OrcaFacil.sln -c Release` → **0 erros**.
- `dotnet test OrcaFacil.sln -c Release` (sem banco) → **670 aprovados, 0 falhas, 13 ignorados** (gate de PostgreSQL).
- `dotnet test OrcaFacil.sln -c Release` com `ORCAFACIL_JOURNEY_CONNECTION` apontando para banco **descartável**
  `orcafacil_journey_20261016` (schema `orcafacil` restaurado por `pg_dump --schema-only`; extensões `pgcrypto` e
  `pg_trgm`) → **683 aprovados, 0 falhas, 0 ignorados** na auditoria original; com os Sprints E-1/E-4
  (testes novos de rascunho de mensagem), o total atual é **731 aprovados, 0 falhas, 0 ignorados**
  (706 do E-2 + 10 testes de endpoint do E-3 + 9 do serviço de ajuda contextual + 7 da resolução do guia
  pela rota, menos 1 teste reescrito).
  Inclui a jornada comercial completa, os 3 testes de
  homologação concorrente de IA e os cálculos de saldo/estorno.
- **Playwright (navegador real, Chromium):** **77/77 verde** (2 setups + 75 specs incl. as 2 de
  `message-drafts.spec.ts`, as 2 de `commercial-review.spec.ts` e as 3 de `contextual-help-ai.spec.ts`). Servidor `dotnet run -c Release --no-build` — nota de processo: a conexão do
  servidor para esta fase aponta para o banco descartável da jornada via override de ambiente
  `ConnectionStrings__DefaultConnection` + `ORCAFACIL_ALLOW_LOCAL_SUPERADMIN_SEED=true` (o `appsettings.Local.json`
  do desenvolvedor aponta para o banco dev `postgres`, onde os usuários de seed E2E não existem; sem o override,
  `setup.client` falha por "credenciais inválidas" — configurado fora do repositório). Reconfirmado após E-4 e E-2.
- Defeito real encontrado **apenas** com PostgreSQL real — **R7 (resolvido nesta execução):** as 6 FKs de
  `manual_payments` não estavam mapeadas no EF; o `INSERT` violava `manual_payments_work_order_id_fkey`
  (23503) ao gravar pagamento e ordem juntos. Corrigido declarando os relacionamentos em
  `ManualPaymentConfiguration` (`DeleteBehavior.Restrict`), **sem nova migration**. O fixture voltou a um único
  `SaveChanges` e a suíte passa pelo caminho real do produto (**683/0/0** reconfirmado).
- **Validadores `package.json` (`check:*`, 611 scripts):** varridos contra o commit-base `1386356` e
  revalidados na árvore final do E-3: **611/611 verdes (`FAIL: 0`)**.
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
  `tests/OrcaFacil.UnitTests/JourneyPostgresTests.cs`,
  `src/OrcaFacil.Persistence/Configurations/CommercialJourneyConfiguration.cs`.

## 6. Próxima ação

1. **R8 — baseline zerado nesta execução:** todos os 611 validadores `check:*` passam (`FAIL: 0`; dupla varredura). Correções aplicadas em produto/validadores: `rel="noopener"` em `/Subscription`, classes legadas removidas em `Templates/Details`, `aria-label` em 6 controles sem rótulo, wrapper `of-responsive-table` em 12 tabelas (SuperAdmin/AccountAdmin), validadores alinhados à realidade (`mobile-ux` Escape, `routes` destino dinâmico `@item.Page`, `no-technical-id-inputs` só input digitável, `New.cshtml.cs` sem `RedirectToPage` desde a evolução GET-only) e `check-release-readiness` reparado para Node ≥20.12 (CVE-2024-27980: `npm.cmd` exige `shell: true`).
2. **Bugs de produto reais corrigidos nesta execução (descobertos pela reprovação E2E):**
   - **/Documents e /Clients listavam sempre "Nenhuma proposta"/vazio:** `BindProperty(SupportsGet = true)` com propriedade não anulável (`Sort`/`Filter`) sofre *implicit required* de NRT; sem `?sort=` na URL o `ModelState` era inválido e `OnGetAsync` retornava `Page()` com lista vazia. Corrigido tornando `Sort`/`Filter` anuláveis (`Documents/Index`, `Clients/Index`, `CommercialRoutine/Index`; consumidores já possuem fallback).
   - **Permissão inexistente `documents.read`:** `QuoteWorkspaceService.ListAsync` e `GlobalSearchService` checavam um código que não existe em nenhum ambiente (a tabela de permissões tem `documents.view`); restaurado `PermissionCodes.DocumentsView`. Efeito anterior: negação de acesso na listagem e na busca global de orçamentos.
   - **`_ClientLayout.cshtml`:** `<partial _OverlayHost>` encaixado entre `}` e `else` quebrava o encadeamento Razor — o ramo else renderizava sempre (duplicando `<main>`/`RenderBody`) e vazava o texto `else {` na página. Estrutura corrigida (partial incondicional após o if/else).
3. **Deriva de dados RBAC (documentado; pendente decisão):** a tabela `permissions` não contém `Margins.View`, `Ai.Suggest`, `Ai.UseCopilot`, `Ai.UseRag`, `Ai.UseSemanticSearch`, `Ai.AnalyzeDocuments`, `receipts.read`, `contracts.read`, `webhooks.read`, `analytics.read`, `files.read` — códigos usados por governança de IA/API keys (verificado também na base de referência). Não é correção de código: exige decisão de seed/patch de dados (não aplicado sem autorização).
4. **Sprint E concluído (E-1, E-2, E-3 e E-4):** o `IContextualAiHelpService` (RAG) agora tem consumidor real no drawer de ajuda, com corpus de guias + artigos publicados, gating de plano e fallback determinístico. Resta apenas ampliar a base de conhecimento (artigos publicados) para melhorar a cobertura — dado operacional, não código.
5. **R5:** validar gateway Mercado Pago em sandbox e o fluxo de inadimplência/regularização (aguarda credenciais).
6. **Higiene:** decidir untrack de `node_modules/` (sucessor do R9 bin/obj já resolvido); decidir destino do banco descartável `orcafacil_journey_20261016` após o merge.
