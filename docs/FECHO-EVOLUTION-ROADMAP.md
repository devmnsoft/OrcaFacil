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

### Sprint P — Catálogo de planos coerente (P0) — **concluído nesta execução**

A auditoria de "todas as funcionalidades dos planos" encontrou divergência real entre
o que é vendido e o que o produto aplica:

- **Matriz de recursos não provisionada.** `script_completop.sql` criava
  `features`/`plan_feature_values`, mas não populava a matriz por plano (a base de jornada só tinha 8 linhas,
  todas de IA). Como `PlanAccessService.CanUseAsync` lê a matriz, Aprovação pública, Ordens de serviço e
  Pagamentos manuais eram **negados para todos os planos** (motivo `FeatureNotConfigured`), e a página
  `/Precos` exibia cards quase vazios.
- **Três recursos exigidos por código sem linha em `features`:** `work_orders.enabled`,
  `payments.manual.enabled` e `receipts.operational.enabled`.
- **Recurso fantasma:** `Program.cs` exigia `documents.create`, que não existe no catálogo → negava a prévia
  de rascunho.
- **Códigos dessincronizados:** 6 recursos existiam só no banco (`documents.monthly_limit`,
  `public_approval.monthly_limit`, `document.convert_to_receipt`, `sharing.public_link`,
  `numbering.custom_prefix`, `support.priority`) e 3 só no código.

Correções (sem `DROP`/`TRUNCATE`, reversíveis):

- `PlanCatalogDefinitions` reescrito como matriz explícita cobrindo **os 29 recursos** nos 4 planos, com
  `PlanFeatureCodes.All` + `DisplayNames` como fonte única.
- `PlanAccessService.GetPlanFeaturesAsync` agora combina banco + matriz (`MergeWithCatalog`): o banco manda e a
  matriz cobre o que ainda não foi provisionado — assim um recurso anunciado nunca é negado por falta de seed.
- `PlanFeatureReadinessService` cobre todos os 29 recursos e distingue `Ready` (gate aplicado) de `Partial`
  (superfície existe mas ainda não consulta o plano), com motivo por recurso. A publicação só bloqueia
  `Missing` ou código desconhecido.
- `Program.cs`: prévia de rascunho passa a usar `PlanFeatureCodes.DocumentsMonthlyLimit`.
- Patch idempotente `database/hotfix_plan_feature_catalog_v75.sql`: garante as 29 linhas de `features`
  (corrigindo o nome exibido de `pdf.watermark` para a marca Fechô) e provisiona as **116 linhas** de
  `plan_feature_values` (4 planos × 29 recursos). Aplicado na base de jornada (`INSERT 0 29` + `INSERT 0 116`).
- Testes: 6 de paridade de catálogo (`PlanCatalogParityTests`) + 2 de provisionamento PostgreSQL
  (`PlanCatalogPostgresSeedTests`) + 2 specs Playwright (`plan-catalog.spec.ts`: diferenciais na página de
  preços e usabilidade no celular sem rolagem horizontal, 375px).

Pendência honesta (registrada no manifesto com status `Partial`): 19 recursos já têm interface mas ainda não
consultam o plano (ex.: `reports.basic`, `exports.csv`, `commercial.*`, `audit.account`, `pdf.watermark`,
`sharing.whatsapp`). Ligar cada gate de rota é o próximo incremento da monetização.

---

## 5. Evidências desta execução

- `dotnet build OrcaFacil.sln -c Release` → **0 erros**.
- `dotnet test OrcaFacil.sln -c Release` (sem banco) → **670 aprovados, 0 falhas, 13 ignorados** (gate de PostgreSQL).
- `dotnet test OrcaFacil.sln -c Release` com `ORCAFACIL_JOURNEY_CONNECTION` apontando para banco **descartável**
  `orcafacil_journey_20261016` (schema `orcafacil` restaurado por `pg_dump --schema-only`; extensões `pgcrypto` e
  `pg_trgm`) → **683 aprovados, 0 falhas, 0 ignorados** na auditoria original; com os Sprints E-1/E-4
  (testes novos de rascunho de mensagem) e do Sprint P (8 testes de catálogo/provisionamento), o total atual é **744 aprovados, 0 falhas, 0 ignorados**
  (706 do E-2 + 11 testes de endpoint do E-3 + 9 do serviço de ajuda contextual + 7 da resolução do guia
  pela rota + 4 do seed de RBAC de IA/margens, menos 1 teste reescrito).
  Inclui a jornada comercial completa, os 3 testes de
  homologação concorrente de IA e os cálculos de saldo/estorno.
- **Playwright (navegador real, Chromium):** **79/79 verde** (inclui as 2 specs de `plan-catalog.spec.ts` do Sprint P) (2 setups + 75 specs incl. as 2 de
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
- **Sprint P (catálogo de planos) — verificado com PostgreSQL e navegador real:** build Release **0 erros**;
  `dotnet test` com banco de jornada **744/0/0** (736 + 8 novos); `plan-catalog.spec.ts` **2/2 verde**
  (diferenciais pagos visíveis em `/Precos`; 3 cards e CTA em 375px sem rolagem horizontal); patch aplicado
  (`INSERT 0 29` recursos + `INSERT 0 116` valores); página conferida visualmente em 375px e 1366px
  (capturas de tela são artefatos locais, fora do versionamento).
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
3. **Deriva de seed de RBAC (corrigida com autorização em 2026-10-19):** `Margins.View`, `Ai.Suggest`, `Ai.UseCopilot`, `Ai.UseRag`, `Ai.UseSemanticSearch` e `Ai.AnalyzeDocuments` são exigidos pelo código (`AiGovernance` / orquestrador de IA) e não existiam em `orcafacil.permissions`, o que deixava copiloto, busca semântica e análise de documentos inutilizáveis para **todos** os papéis. Correção aditiva, idempotente e sem `DROP`/`TRUNCATE`:
   - migração `20261019120000_GrantAiGovernancePermissions` (registrada em `dotnet ef migrations list`, `--no-connect`);
   - patch `database/hotfix_ai_governance_permissions_v74.sql` aplicado na base de jornada (`INSERT 0 6` permissões + `INSERT 0 22` concessões);
   - papéis derivados do catálogo existente, não inventados: os códigos `Ai.*` seguem `Ai.View` (Owner, Administrator, Manager, Commercial) e `Margins.View` segue `Finance.View` (Owner, Administrator); o gate de custo/margem segue duplo (permissão + `AllowCostAndMargin` do plano);
   - 4 testes PostgreSQL de guarda (`RbacAiGovernanceSeedTests`) travam a regra contra nova divergência;
   - `receipts.read`, `contracts.read`, `webhooks.read`, `analytics.read` e `files.read` **não** são permissões de RBAC: são escopos de chave de API (`ApiKeyService.AllowedScopes`) e os demais escopos (ex.: `clients.read`) também não têm linha em `permissions` — por isso ficaram de fora do seed.
4. **Sprint E concluído (E-1, E-2, E-3 e E-4):** o `IContextualAiHelpService` (RAG) agora tem consumidor real no drawer de ajuda, com corpus de guias + artigos publicados, gating de plano e fallback determinístico. Resta apenas ampliar a base de conhecimento (artigos publicados) para melhorar a cobertura — dado operacional, não código.
5. **R5 — parte do ciclo de inadimplência fechada nesta execução (2026-10-20):**
   - **Correção real de carência fictícia:** as versões de plano eram semeadas sem `grace_period_days`
     (DEFAULT 0). O `BillingStatusService` anunciava "carência de 3 dias" no evento de atraso, mas o
     `PlanAccessService` lê somente `plan_versions.grace_period_days` — com 0 dias, os benefícios pagos eram
     cortados no mesmo instante do vencimento. Correção aditiva e idempotente: seeds
     (`script_completop.sql`/`import_orcafacil.sql`) agora provisionam 3 dias nos planos pagos (0 no FREE,
     coerentes com `BillingOptions.GracePeriodDays`), e o patch `database/hotfix_plan_grace_period_v76.sql`
     corrige versões publicadas em bancos existentes (nunca reduz valor já configurado).
   - **Prova ponta a ponta (sem gateway):** `DunningCycleHomologationTests` trava o ciclo
     pago completo — PastDue na carência com benefícios ATIVOS, plano efetivo cai para FREE ao fim da
     carência, suspensão após o prazo, restauração automática pelo worker com cobertura comprovada
     (`billing.restored`) e benefícios de volta no plano pago; além de assinatura quitada pelo webhook que
     não regride no sync e o congelamento do contrato carência anunciada = carência efetiva.
     Suíte: 728 aprovados, 0 falhas (19 ignorados por gate de PostgreSQL); build Release 0 erros;
     validadores `check:database-schema`, `check:database-schema-contract`, `check:billing-flow`,
     `check:data-migration` e `check:database-connection-config` verdes.
   - **Divergência de base nova descoberta e corrigida (PostgreSQL real, 2026-10-20):** a suíte de jornada
     executada contra um banco PostgreSQL descartável provisionado exclusivamente por
     `database/script_completop.sql` revelou que o consolidado **não encadeava** os patches de RBAC de IA
     (v74), catálogo de planos (v75) e carência mínima (v76) — a cadeia `\ir` parava no v73. Uma go-live nova
     pelo runbook oficial nasce com catálogo incompleto (26 recursos/8 valores/4 permissões Ai.*/sem
     Margins.View/carência 0) e **falha nos próprios guards** (`RbacAiGovernanceSeedTests`,
     `PlanCatalogPostgresSeedTests`). Correção: os três patches passam a encadear no fim do consolidado
     (aditivos, idempotentes — segunda aplicação sem erro e sem duplicação). Base nova do consolidado: 29
     recursos, 116 valores (4 planos × 29), 9 permissões Ai.* + Margins.View, carência paga 3 dias.
   - **Instrumento e runbook prontos nesta execução (2026-10-20):**
     `scripts/sandbox/mercadopago-sandbox.mjs` (CLI sem dependências: cria Pix/boleto sandbox, consulta
     status e envia webhooks assinados no formato oficial `ts/v1` — inclusive a moldura `--tamper` que
     falha se a aplicação aceitar assinatura inválida, e `--echo` para conferência offline do HMAC contra
     verificador independente) e `docs/SANDBOX-MERCADOPAGO-R5.md` (passos, SQL de verificação, matriz de
     aceites e critério de fechamento). Executar a matriz exige apenas as credenciais TEST- no ambiente.
   - **E-mail transacional de cobrança (novo nesta execução):** toda transição real do ciclo de inadimplência
     agora avisa o titular — `billing.overdue` (carência com benefícios ativos), `billing.grace_ended`,
     `billing.suspended` (benefícios pausados, dados preservados) e `billing.restored`. Implementação:
     `IBillingNoticeService` (Application, política em `BillingStatusService.NoticeEventTypes` — liberação
     manual e estado de teste nunca viram e-mail de cobrança) + `BillingNoticeEmailService` (Web) escrevendo
     na outbox com destinatário Owner ativo da conta, payload protegido por Data Protection e **idempotência**
     por `IdempotencyKey` única (`evento:assinatura:dia`) — o worker nunca repete o mesmo aviso. Contrato
     travado em `BillingNoticeContractTests` (aviso só por transição real, só após persistência, sem ruído
     em varreduras estáveis).
   - **Resta (execução):** rodar a matriz de aceites do runbook com credenciais sandbox e registrar a
     evidência.
7. **Monetização (próximo incremento do Sprint P):** ligar o gate de plano nos 19 recursos que já têm interface
   (`reports.basic`/`advanced`, `exports.csv`, `commercial.pipeline|followups|metrics`, `audit.account`,
   `branding.custom_logo`, `templates.custom_enabled`, `sharing.whatsapp`, `pdf.watermark` e os limites de
   clientes/serviços/equipe/templates), reclassificando-os de `Partial` para `Ready` no manifesto de prontidão.
6. **Higiene — untrack de `node_modules/` e `.vs/` concluído nesta execução (2026-10-20):** commit `e0f4568`
   removeu 1157 arquivos rastreados (sucessor do R9 bin/obj) mantendo os arquivos no disco; `.vs/` (estado do
   Visual Studio, incl. DBs do Copilot) entrou no `.gitignore`; `check:git-safety` verde; índice caiu de 3324
   para 2167 arquivos. Pendente: decidir o destino do banco descartável `orcafacil_journey_20261016` após o merge.
8. **Convites de equipe por e-mail (item 1 de `NEXT-RELEASE.md`) concluídos nesta execução (2026-10-21):**
   commit `26cf4e6`. Antes, o único caminho era senha temporária digitada pelo admin (a pessoa nunca era
   avisada por e-mail). Agora o admin marca **"Enviar convite por e-mail"** na criação de usuário
   (`/AccountAdmin/Users/New`) e o convidado define a própria senha no aceite. Implementação:
   - **Entidade `AccountInvitation`** (`orcafacil.account_invitations`): token cru nunca persistido (só
     hash SHA-256 com pepper), aceite ÚNICO com validade de 7 dias, revogação que expira no ato sem apagar
     histórico (sem `DROP`/`DELETE`); migração EF estritamente aditiva (`20261021120031` — o gerador
     detectou drift pré-existente de `budget_templates`/`manual_payments`/`ck_users_plan` e foi enxugado
     manualmente para criar somente a tabela nova) + tabela no `script_completop.sql` com índices.
   - **Fluxo completo:** convite no `AccountInvitationService` (reemissão revoga pendentes anteriores do
     mesmo e-mail; e-mail já cadastrado no Fechô vira vínculo direto, não convite), e-mail transacional na
     outbox (idempotente por convite, template `account-invite`) e página pública `/Auth/AcceptInvite`
     (dados por token, senha própria, termos de uso, aceite único em transação serializável, login direto).
   - **Limite de vagas honesto:** convite pendente OCUPA vaga (`AccountInvitationRules.TeamCapReached`) —
     membros ativos + pendentes >= `TeamMembersLimit` bloqueia com mensagem de upgrade; o limite não pode
     mais ser furado por enfileirar convites.
   - **Evidência:** 743 aprovados, 0 falhas sem DB (13 testes novos em `AccountInvitationRulesTests`
     travando cânon de vagas, perfis atribuíveis, unicidade/expiração/revogação do token); build 0 erros;
     validadores `check:database-schema`, `check:database-schema-contract`, `check:billing-flow`,
     `check:data-migration`, `check:git-safety`, `check:admin-billing` verdes.
   - **Resta (execução com DB real):** homologação visual do aceite num PostgreSQL descartável; caso de
     borda registrado: se o e-mail do convidado se cadastrar sozinho depois do convite, o aceite cai no
     caminho de vínculo à conta existente (sem trocar senha).
