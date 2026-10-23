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
| Ordem de serviço | `CommercialJourneyService` (conversão, espera), `WorkOrderStatusTransitionService`, `WorkOrderOverdueService` | `work_orders`, `work_order_checklist_items` | `Pages/WorkOrders/*` | conversão idempotente, estados (espera + vencimento alcançáveis), checklist, agenda | testes de transição, `check:work-orders` | **Funcional** |
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
7. **Monetização — gates de plano nos 19 recursos concluídos (gates em 2026-10-19; fechamento com prova em
   2026-10-19→21):** os 29 recursos do catálogo estão `Ready` no manifesto de prontidão, e a verificação desta
   execução **encontrou e corrigiu três alegações `Ready` sem enforcement nenhum no código**:
   - `clients.active_limit` / `services.active_limit`: nenhuma tela consultava o plano — agora `/Clients/Create`
     e `/Services/Create` chamam `CanUseAsync` antes de criar (bloqueio com o teto do plano na mensagem, na
     mesma linha de estilo do limite de equipe); o contador de serviços media eventos `ServiceActivated` que
     **ninguém grava** (uso sempre zero) e passou a contar os itens ativos do catálogo.
   - `pdf.monthly_limit`: a constante não era usada em lugar algum — a cota Free de 10 PDFs/mês agora é
     aplicada em `/Documents/Pdf/{id}` (Web) e `DocumentsController.Pdf` (API) por **limite+uso direto**
     (`GetLimitAsync`/`GetUsageAsync`), sem reabrir a política de consulta histórica pós-trial; o gasto é
     registrado em `user_usage` via `RegisterPdfAsync` (antes código morto, agora com `AccountId`), e a
     contagem soma os PDFs das pessoas **ativas da conta** (`user_usage` × `account_members`) — não pode ser
     contornada trocando de conta e erra para cima, nunca para baixo. Bônus: o registro de auditoria
     `PDF_GENERATED` da API era adicionado ao contexto e **nunca salvo** — agora persiste.
   - **Prova (novo validador `check:feature-gates`):** para cada entrada `Ready`, exige que a constante do
     recurso seja usada fora das definições por um arquivo que chama `CanUseAsync`/`GetLimitAsync` (ou
     `RequiredFeature` com enforcement na base); a marca d'água do PDF é exceção declarada verificada por
     tokens (gate por plan type `Free` no template, coerente com a matriz 1 Enabled/3 Disabled). Também
     trava **zero entradas `Partial`**. Verde: 29/29, suíte 743/0/19, build 0 erros.
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
9. **PWA (item 2 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-21):** commit `89e3573`. O esqueleto
   já existia (manifest, service worker, banner de instalação, página `/Offline`) mas faltava o que torna o
   app **instalável de fato** e faltava evidência. Nesta execução:
   - **Ícones rasterizados com honestidade:** `fecho-pwa-192.png`, `fecho-pwa-512.png` e
     `apple-touch-icon.png` (180) gerados por renderização do próprio `fecho-symbol.svg` em canvas do
     browser (fundo full-bleed `#0B3835`, símbolo em 80% — zona segura maskable), não desenhos
     aproximados. O iOS ganhou `apple-touch-icon` nos dois layouts (antes usaria screenshot da página).
   - **Manifest instalável:** PNG 192/512 declarados com `sizes` e `purpose` reais (antes só SVG);
     dimensões verificadas lendo o cabeçalho IHDR dos bytes, não confiando só na declaração.
   - **Faixa de segurança do SW preservada e travada:** rotas privadas (`Auth/Settings/Api/Admin/...`)
     fora do cache, navegação em rede com fallback `/Offline` e **nunca** cache de navegação (conteria
     dados de sessão); banner de atualização pede recarga (nunca recarrega sozinho); envio de formulário
     offline é bloqueado, não simulado.
   - **Evidência:** novo validador `check:pwa` (`scripts/check-pwa.mjs`) — manifest, arquivos de ícone com
     dimensões reais, 28 assets de precache existentes, SENSITIVE_PATH, fallback offline, registro nos dois
     layouts e honestidade dos banners; verde. Suíte 743 aprovados, 0 falhas; build 0 erros;
     `check:git-safety`, `check:authenticated-design`, `check:public-design` verdes.
10. **WhatsApp oficial — Cloud API do Meta (item 4 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `862fa3e`.
    O canal anterior era `WhatsAppPreparedText`: gerava um link `wa.me` e gravava a mensagem como
    `Prepared`. O envio nunca saía do produto. Esta entrega liga o canal real, e o critério de aceite é o
    mesmo dos demais itens: **nada é declarado como enviado sem evidência do provedor**.
    - **Envio real (`WhatsAppGraphClient` → Graph API v21.0):** `POST /{phone_number_id}/messages` com
      `messaging_product=whatsapp` e Bearer do token da conta. Rede indisponível, status não-2xx ou corpo
      sem `messages[0].id` viram `Succeeded=false` com erro sanitizado — nenhum desses caminhos grava
      `Sent`. A mensagem é registrada `Queued` e só muda de estado com o **callback do próprio Meta**
      (`sent`/`delivered`/`read`/`failed`), nunca por conta do envio.
    - **Webhook com assinatura (`/api/webhooks/whatsapp`):** GET é o handshake do Meta (só responde quando
      o `hub.verify_token` pertence a uma conta **habilitada**); POST é verificado por HMAC-SHA256 com o
      **app secret da própria conta** (`X-Hub-Signature-256`, comparação em tempo constante). Assinatura
      inválida ou `phone_number_id` sem conta habilitada são recusados com 401 — o evento não toca em dado.
    - **Duas armadilhas de deduplicação corrigidas antes de virar incidente** (ambas testadas):
      (a) a chave era `object:entry.id`, mas o `entry.id` do Meta **é constante por conta** (é o phone
      number id) — a segunda mensagem real seria descartada como "duplicada" e nunca chegaria à caixa de
      entrada; (b) os callbacks de status de uma mesma mensagem compartilham o `id`, então `sent`,
      `delivered` e `read` cairiam na mesma chave e **o status nunca avançaria**. A chave passou a ser por
      conteúdo (id de mensagem; id **+ estado** de status), com fallback determinístico por hash do corpo;
      o hash curto deduplica em `event_type varchar(40)` e a chave completa fica em `provider_reference`.
    - **Correções que só apareceriam em produção:** `NormalizePhone` era um no-op (os dois ramos do
      ternário devolviam `digits`) e mandava número local para a API; agora aplica DDI 55 só a nacional
      com DDD, sem adivinhar país. O serviço de envio lia a conta com `AsNoTracking` e **mutava** `Status`/
      `LastFailure` — as atualizações de saúde nunca persistiam. Mensagens recebidas eram gravadas sem
      encode, contra a disciplina do resto da caixa de entrada; não-texto virava linha vazia, agora registra
      o tipo real recebido (`[image]` etc.).
    - **Configuração auditada:** nova seção "WhatsApp Cloud API" em `/Settings/Integrations` (handler
      próprio, permissão `IntegrationsManage`). Access token e app secret são gravados **protegidos**
      (mesmo propósito `OrcaFacil.Omnichannel.WhatsApp.v1` em gravação e leitura), nunca voltam à tela —
      a tela só mostra "já salvo"; habilitar exige os dois segredos (sem app secret a assinatura nunca
      passaria). A URL do webhook é exibida para colar no app da Meta, e todo salvamento gera auditoria.
      O opt-out comercial é checado antes de qualquer envio.
    - **Schema:** `omnichannel_whatsapp_accounts` com colunas tipadas (migration `20261021165111` +
      snapshot) e hotfix idempotente `hotfix_omnichannel_whatsapp_api_v77.sql` **encadeado no
      `script_completop.sql`** — antes, só o patch aplicava e a base nova nascia sem o módulo.
    - **Evidência:** novo validador `check:whatsapp-api` (10 grupos de contrato, incluindo a presença dos
      testes das regressões que já doeram) — verde, e falha ao se reintroduzir a deduplicação antiga
      (mutação verificada). 32 testes novos em `WhatsAppCloudApiContractTests` (normalização, assinatura,
      dedup por conteúdo e por estado, cliente sem simulação de sucesso, DI). Suíte **775 aprovados,
      0 falhas, 19 ignorados** (gate de PostgreSQL pré-existente); build Release 0 erros, sem avisos novos;
      `check:omnichannel-flow`, `check:support-flow`, `check:integrations`, `check:di-consistency`,
      `check:aspnet-scope`, `check:permissions`, `check:secrets-masking`, `check:quality-gate-no-fake` verdes.
    - **Resta (fora deste repositório):** homologar handshake e envio com um número real no sandbox da
      Meta (credenciais fora do repositório, mesmo critério do R5). Nenhum envio de produção foi feito ou
      simulado aqui.

11. **Ordem de serviço — estados de espera e vencimento alcançáveis (item 5 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `0aa3a3d`.
     A matriz marcava o módulo como Funcional, mas a auditoria do código achou uma lacuna real: `WaitingCustomer`,
     `WaitingMaterial` e `Overdue` existiam na tabela de transições e eram lidas pelo dashboard, porém **nada no
     sistema conseguia gravá-las**. O KPI de "serviços atrasados" (`AnalyticsV21Service`) lia `Overdue` e ficava
     sempre zero; o operador não tinha como registrar que a ordem parou por espera do cliente ou por falta de
     material — só Pausar, que é outro significado (pausa interna, sem motivo externo auditável).
    - **Espera com motivo auditável:** `WaitAsync` na jornada comercial move a ordem para `WaitingCustomer` ou
      `WaitingMaterial` gravando motivo, data de início e observação. **Retomar limpa o estado** — sem isso a
      próxima ordem herdaria o motivo antigo e o relatório mentiria. A regra pura `WorkOrderWaitRules` (motivo →
      status) foi extraída do serviço que depende de `DbContext`, para ser testável sem banco, como os demais
      rules do repositório.
    - **Vencimento por worker, não por adivinhação:** `WorkOrderOverdueService` marca em lote, multi-conta, as
      ordens `Scheduled` cujo fim planejado já passou, deixando evento na timeline. `WorkOrderOverdueWorker` roda
      a cada 15 min sob `IJobLockService` (mesmo desenho do `BillingBackgroundWorker`), com trava de concorrência
      e checagem de banco configurado. O critério é `Scheduled` + `ScheduledEnd < now`: nunca vence ordem já em
      execução, concluída ou cancelada.
    - **UI que o operador consegue usar:** em execução, botão "Mover para espera" (cliente/material + nota); em
      espera, exibe **desde quando** e **por quê**, e oferece retomar. Sem exibir o tempo de espera o operador
      decide às cegas se ainda vale cobrar o cliente.
    - **Schema:** `wait_reason`, `waiting_since`, `wait_note` e `overdue_marked_at` (migration
      `20261023120000` + snapshot EF) e hotfix aditivo/idempotente `hotfix_work_order_wait_overdue_v78.sql`
      **encadeado no `script_completop.sql`**. `WaitReason` é enum convertido para `varchar(40)` como os demais
      enums do módulo — sem a conversão a gravação falharia **só em produção**, não no build.
    - **Evidência:** novo validador `check:work-orders` (8 grupos de contrato) — verde, e provado por mutação que
      pega o regresso de "retomar sem limpar o estado de espera". 17 testes novos em `WorkOrderWaitAndOverdueTests`
      (espera alcançável, retomada, proibição de concluir direto da espera, vencimento a partir do agendamento,
      vencida ainda iniciável/cancelável, concluída/cancelada imune a vencimento, motivo `None` rejeitado). Suíte
      **792 aprovados, 0 falhas, 19 ignorados** (gate de PostgreSQL pré-existente); build Release 0 erros, sem
      avisos novos; `check:omnichannel-flow`, `check:commercial-journey`, `check:database-schema-contract`,
      `check:di-consistency`, `check:service-lifetimes`, `check:aspnet-scope`, `check:quality-gate-no-fake`,
      `check:build-blockers`, `check:git-safety` verdes.
12. **Assinatura recorrente do cliente — cobrança das competências automática (item 6 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `9386fee`.
     O módulo de contratos já tinha `NextBillingDate`, `ContractPayment`, `ContractRecurrenceRun` e a leitura de
     atraso e expiração, mas a auditoria achou o mesmo padrão do item 5: **a cobrança recorrente não andava sozinha**.
     `NextBillingDate` era gravada na criação e nunca avançava; nenhuma competência era gerada; `Overdue` e
     `Expired` só existiam como contagem calculada na leitura. Sem geração automática, a "assinatura" era um
     formulário de previsão manual.
    - **Regras puras `RecurringBillingRules`:** período em meses, avanço do vencimento mantendo o dia e grampeado
      ao fim do mês (31 de janeiro vira 28 de fevereiro), quando gerar, quando vence e quando expira. Período
      personalizado sem meses positivos é recusado (senão avançaria zero meses em loop).
    - **Geração com o mesmo rastro financeiro do caminho manual:** cada competência cria `ContractPayment`
      pendente e o `FinancialEntry` vinculado, então a cobrança aparece em Financeiro e no relatório recorrente.
    - **Sem cobrança duplicada:** competência já existente (inclusive a lançada à mão) é pulada; o índice único
      (conta, contrato, competência) segura a corrida entre instâncias; o avanço de `NextBillingDate` impede
      reprocessar a mesma data. Teto de 12 competências por rodada, para o catch-up após reativação não virar
      rajada.
    - **Atraso e expiração gravados:** cobrança pendente com vencimento passado vira `Overdue` (e o lançamento
      financeiro acompanha); contrato ativo com prazo encerrado vira `Expired`. Cobrança paga, cancelada, ou de
      contrato pausado/suspenso não é tocada.
    - **Worker de hora em hora** (`RecurringContractBillingWorker`) sob `IJobLockService`, na **data civil de São
      Paulo**. Usar o UTC do servidor cobraria até 3 horas antes do vencimento.
    - **Sem migração:** todas as colunas e status já existiam no schema; o status `Overdue` cabe no `varchar(24)`.
    - **Evidência:** validador `check:recurring-billing` (7 grupos), provado por mutação que pega a remoção do
      avanço de `NextBillingDate` (o defeito que faria cobrar a mesma competência a cada hora). 32 testes novos em
      `RecurringBillingRulesTests`, incluindo fim de mês, contrato pausado, término no próprio dia e cobrança paga.
      Suíte **824 aprovados, 0 falhas, 19 ignorados** (gate de PostgreSQL pré-existente); `check:work-orders`,
      `check:commercial-journey`, `check:database-schema-contract`, `check:di-consistency`,
      `check:service-lifetimes`, `check:aspnet-scope`, `check:quality-gate-no-fake`, `check:build-blockers` verdes.
    - **Limites conhecidos (não fechados aqui):** (a) eventos automáticos vão para a atividade geral, não para a
      linha do tempo do contrato, porque `ContractEvent.UserId` é obrigatório e não há usuário na geração;
      (b) "Ativar" um contrato já expirado, sem ajustar a vigência, é expirado de novo na rodada seguinte;
      (c) `PendingRenewal` e `Suspended` continuam sem transição automática. Renovação segue manual.
      (d) a homologação com cobrança real (boleto/Pix) é o item 7, "cobrança do cliente".

13. **Cobrança do cliente — lembretes de recebimento vencido (item 7 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `0db02bb`.
      A auditoria separou o que já existia do que faltava: a assinatura **recorrente** (item 6) já gera a competência e marca
      `Overdue`, e a cobrança da assinatura SaaS da conta própria é outro fluxo (`IBillingNoticeService`). Mas **nada avisava o
      cliente final sobre o próprio recebimento vencido** — o `Overdue` do item 6 não disparava nada, e recebimento de OS,
      orçamento ou manual vencido nem era marcado (ficava `Pending`). Escopo fechado aqui: **lembrete sem repasse de dinheiro**;
      cobrar via boleto/Pix por tenant segue adiado.
     - **Sem mover dinheiro (critério de escopo):** o lembrete só diz ao cliente quanto falta; não cria pagamento, não altera
       `Amount`/`PaidAmount` e não toca em gateway. Repasse com chave Mercado Pago por conta (critério R5) fica para depois.
     - **Regras puras `ReceivableReminderRules`:** recebimento aberto exige status em aberto + saldo (pago, cancelado ou sem saldo
       não é lembrado); janela de grace após o vencimento; intervalo mínimo entre lembretes; teto que encerra a cobrança automática;
       nunca lembra antes do vencimento. Decididas por **data civil**, como o item 6.
     - **Dois canais, opt-out respeitado:** e-mail entra na outbox (`EmailOutboxMessage`) com chave de idempotência por
       (recebimento, dia) e só quando o envio está habilitado na conta; WhatsApp reutiliza `OmnichannelWhatsAppApiService.SendAsync`,
       que já checa conta configurada + opt-out comercial; opt-out de e-mail lido em `OmnichannelOptOutEvent` (canal "Email").
       Quem não tem canal alcançável é pulado **sem contar** — para lembrar quando o contato aparecer.
     - **Cadência no próprio recebimento:** `FinancialEntry` ganha `LastReminderAt` (data civil) e `ReminderCount`; a rodada grava
       a atividade `ReceivableReminded` e atualiza os dois campos, então nenhuma varredura repete o mesmo envio.
     - **Worker de hora em hora** (`ReceivableReminderWorker`) sob `IJobLockService`, na data civil de São Paulo.
     - **Configuração:** seção `ReceivableReminder` (`Enabled`/`GraceDays`/`IntervalDays`/`MaxReminders`) registrada no
       `Program.cs` e documentada em `appsettings.Example.json` (defaults: grace 3, intervalo 7, teto 3).
     - **Schema:** migration `20261023130000_AddFinancialEntryReminderState` + snapshot EF + hotfix idempotente
       `hotfix_financial_entry_reminder_v79.sql` **encadeado no `script_completop.sql`**.
     - **Evidência:** validador `check:receivable-reminder` (8 grupos), provado por mutação que pega a remoção da cadência, do
       incremento do contador, do opt-out de e-mail e do gate de envio habilitado. 14 testes novos em `ReceivableReminderRulesTests`
       (aberto/saldo, nunca antes do vencimento, intervalo, teto, decisão completa). Suíte **838 aprovados, 0 falhas,
       19 ignorados** (gate de PostgreSQL pré-existente); `check:database-schema-contract`, `check:service-lifetimes`,
       `check:aspnet-scope`, `check:quality-gate-no-fake`, `check:build-blockers`, `check:commercial-journey`,
       `check:work-orders`, `check:recurring-billing` verdes.
     - **Limites conhecidos (não fechados aqui):** (a) cobrança com **repasse de dinheiro** (boleto/Pix por tenant, chave do
       Mercado Pago por conta — critério R5) continua manual; (b) duplo envio raro de WhatsApp num crash entre o envio e o
       `SaveChanges` final — o e-mail, esse, está protegido pela idempotência; (c) cadência e limite são globais por deploy, ainda
       não por conta.

14. **Agenda operacional — data civil na zona de negócio e conflitos de horário visíveis (item 8 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `348a170`.
      A auditoria achou dois defeitos reais na página `Pages/Schedule`: as visões hoje/semana bucketizavam pelo **dia UTC do servidor**
      (serviço noturno caía no dia errado — mesma classe do bug que o item 6 corrigiu no billing recorrente) e as horas eram exibidas
      no fuso local da máquina. Além disso, a dobra do mesmo responsável só aparecia ao salvar, nunca na grade. Escopo fechado aqui:
      corrigir a data civil e tornar o conflito visível, **sem tocar em schema**.
     - **Regras puras `ScheduleAgendaRules`:** `BusinessToday`/`CivilDayStartUtc`/`Window` decidem o dia de negócio em America/Sao_Paulo
       e projetam a meia-noite local para UTC; `FindConflicts` marca sobreposição **estrita** de janela para o **mesmo responsável**, com
       a mesma semântica de `FieldScheduleService` (fronteira comum não conflita). Sobrecargas com `TimeZoneInfo` explícita permitem provas
       hermeticamente determinísticas nos testes.
     - **Página:** hoje/semana filtrados pela janela civil; atrasadas/sem agenda mantidas; horas exibidas já projetadas para a zona de negócio;
       badge "Horário sobreposto" por serviço + resumo de quantos conflitam antes de despachar.
     - **Evidência:** `check:schedule-flow` estendido (bucketing civil, exibição na zona de negócio, conflito visível) e provado por mutação
       (7 mutações pegadas). 9 testes novos em `ScheduleAgendaRulesTests` (dia civil ≠ dia UTC, meia-noite local → UTC, janela, fronteira
       comum, conflito apenas no mesmo responsável). Suíte **847 aprovados, 0 falhas, 19 ignorados**; regressões verdes (schema-contract,
       service-lifetimes, aspnet-scope, quality-gate, build-blockers, commercial-journey, work-orders, recurring-billing,
       receivable-reminder, design-consistency, razor-pages, routes, authenticated-navigation).
     - **Limites conhecidos (não fechados aqui):** (a) o detector roda sobre as ≤100 linhas já carregadas na visão, então um conflito com
       um serviço fora do recorte pode não aparecer ali — a checagem autoritativa continua sendo a do save via `FieldScheduleService`;
       (b) conflito é por `AssignedUserId` (um único recurso), ainda não por equipe; (c) a exibição segue em lista, não em grade visual de horas.

15. **Integração fiscal — fluxo manual de fiscalização de recebíveis pagos (item 9 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `c429624`.
       A auditoria achou uma base fiscal rica porém órfã: `FiscalModule.cs` traz dezenas de serviços e `FiscalRules`, provedores
       `Noop`/`Manual` e um certificado protegido, e a entidade `FiscalDocumentRequest` (recebível/pagamento/recibo → número/chave
       mascarada/provedor) já mapeada em tabela — mas **nenhum serviço a usava e não existia página `/Fiscal`**. O produto, por design,
       não emite nota fiscal ("nenhum número ou chave fiscal é fabricado; credenciais futuras por env/cofre"). Escopo fechado aqui:
       **fluxo manual de fiscalização** sobre a base existente, sem fabricar dado fiscal e sem migração.
      - **Sem mover dinheiro, sem fabricar dado fiscal:** o operador registra o número e a chave que ele mesmo emitiu; o provedor é
        estampado como "Controle manual auditado" (nome do `ManualFiscalProvider`), nunca como autorização de prefeitura. A chave de
        acesso é armazenada mascarada (somente as pontas), via `FiscalRequestRules.MaskAccessKey`.
      - **Regras puras `FiscalRequestRules`:** recebível só é fiscalizável com valor recebido e sem cancelamento; mapa de transições
        `CanTransition` (aberto → emitida/cancelada/sem necessidade; emitida → cancelada); estados terminais não reabrem; `IsActive`
        garante idempotência por solicitação aberta; motivo auditável (≥10 chars, reutilizando `FiscalRules.RequireReason`) exigido
        para cancelar ou marcar como sem necessidade.
      - **Serviço `FiscalDocumentWorkflowService`:** solicita, registra emissão, cancela e marca como sem necessidade sobre
        `FiscalDocumentRequest`, sempre isolado por conta (`FiscalRules.EnsureSameAccount`), idempotente enquanto houver pedido aberto
        para o mesmo recebível, com evento de atividade (`ActorUserId`) e `Touch` ao mudar de estado.
      - **Página `/Fiscal`** (permissão `FinanceManage`, isolada por conta): lista pedidos por status, registra número/chave/data da
        emissão, cancela ou marca como sem necessidade, e destaca o gap "Recebido sem nota fiscal" (recebíveis pagos sem nota
        emitida). A ressalva contábil é exibida (`FiscalRules.AccountingDisclaimer`).
      - **Configuração/DI:** `IFiscalDocumentWorkflowService` registrado como scoped no `Program.cs`. Sem chave nova em `appsettings`
        (o modo manual não requer credencial externa) e sem migração — todas as colunas já existiam.
      - **Evidência:** validador `check:fiscal-flow` (6 grupos), provado por mutação (3 mutações pegadas: número fabricado, gap
        invisível, estado terminal reaberto). 33 testes novos em `FiscalRequestRulesTests` (fiscalizável, estados abertos/terminais,
        matriz completa de transições, emitida-cancela-mas-não-reemite, número válido, máscara de chave). Suíte **880 aprovados,
        0 falhas, 19 ignorados** (gate de PostgreSQL pré-existente); regressões verdes (schema-contract, service-lifetimes,
        aspnet-scope, build-blockers, commercial-journey, design-consistency, razor-pages, routes, authenticated-navigation,
        fiscal-flow).
      - **Limites conhecidos (não fechados aqui):** (a) a emissão real (NFS-e via provedor plugável, `INfseProvider`, atrás de
        credencial por env — critério R5) continua manual; (b) a âncora é o recebível `FinancialEntry` — pagamento/recibo avulso sem
        lançamento ainda não ganha nota; (c) sem índice único em `(AccountId, ReceivableId)`: a idempotência é em nível de serviço
        (um pedido aberto por recebível), suficiente para a operação de um único operador.

16. **Aplicativo móvel — experiência app-grade no PWA instalado (item 10 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `b3a5839`.
       O audit mostrou que o único "móvel" era o PWA do item 2 — instalável, porém conservador: manifest sem atalhos de
       lançamento rápido e sem "casca" de app quando instalado; a navegação mobile seguia só via menu hambúrguer. Não há
       projeto nativo (MAUI/Capacitor/RN). Escopo fechado aqui: **experiência app-grade sobre o PWA existente**, sem toolchain
       nativa e sem tocar na faixa de segurança do service worker (rotas autenticadas continuam fora do cache).
      - **Atalhos de lançamento rápido:** `site.webmanifest` ganha `shortcuts` para Início (`/Dashboard`), Agenda (`/Schedule`),
        Ordens (`/WorkOrders`), Pagamentos (`/Payments`) e Fiscal (`/Fiscal`) — no Android/iOS o app instalado abre direto nesses
        fluxos de campo.
      - **Bottom tab bar de campo:** `mobile-app.css` + `mobile-app-shell.js` mostram uma barra de abas inferior com os 5
        atalhos, **somente** quando o app roda em modo standalone (instalado) E a tela é pequena (<= 820px); em desktop/
        navegador fica inerte e a navegação por menu permanece. A barra respeita `env(safe-area-inset-bottom)` (notch) e dá
        inset ao `.of-main` para nada ficar escondido atrás dela.
      - **Estado atual visível:** o script destaca o tab ativo pela rota (`data-path-prefix` + `aria-current`) e re-avalia em
        `resize`/`orientationchange`.
      - **Integração segura:** ligada no `_Layout.cshtml` só dentro da guarda de autenticação; os tokens exigidos pelo
        `check:pwa` (manifest, apple-touch-icon, theme-color, pwa.js, pwa-install.js, offline-status.js) continuam intactos.
        Sem chave nova em appsettings e sem migração.
      - **Evidência:** validador `check-mobile-app` (coleta todas as falhas), **provado por mutação** (4 mutações em 4 arquivos
        capturadas: rota de shortcut inválida, tab bar visível por padrão, shell ativada sem limite de largura, atalho de campo
        ausente). Cobertura front-end via validador — não há runner de testes JS no repositório (mesmo papel do `check:pwa`).
        Suíte C# inalterada **880 aprovados, 0 falhas, 19 ignorados**; regressões verdes (pwa, routes, razor-pages,
        design-consistency, authenticated-navigation, service-lifetimes, aspnet-scope, build-blockers).
      - **Limites conhecidos (não fechados aqui):** (a) os assets da shell não entram no precache do SW — offline segue com o
        fallback `/Offline`; (b) ainda não há app nativo para loja (Capacitor) nem notificações push — candidatos a incrementos
        próprios; (c) a barra complementa (não substitui) o menu superior em standalone, que mantém acesso a menu/sair/
        notificações.

17. **API pública — leitura dos recursos de negócio (item 11 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `723d29e`.
       O audit mostrou que a API pública V1 já tinha base sólida — auth por chave de API (hash SHA-256), rate-limit, paginação,
       idempotência, erro consistente (ApiError), OpenAPI e UI de gestão de chaves em Settings/ApiKeys. O gap era preciso:
       ApiKeyService.AllowedScopes JÁ declarava escopos sem endpoint (quotes.read/write, work_orders.read, receipts.read,
       contracts.read) e o catálogo /webhooks/events já prometia eventos de orçamento/ordem/recibo/contrato sem rota.
       Escopo fechado aqui: **API de leitura** desses recursos, reutilizando os escopos já declarados.
      - **Novos endpoints (leitura):** GET /quotes (+/{id}), GET /work-orders (+/{id}), GET /receipts (+/{id}) e GET /contracts (+/{id}),
        cada um com lista paginada + detalhe. Orçamentos = DocumentType.Budget; contratos = RecurringContract.
      - **Mesmo contrato da V1:** todos isolados por conta (Account(user)), AsNoTracking, paginados e com o mesmo ApiError/Pagination;
        enums devolvidos pelo nome (ClientDecision, WorkOrderStatus, ContractStatus etc., via ToString em memória para não
        depender de tradução no provider).
      - **Sem migração, sem novo escopo:** reusa os 4 escos de leitura já existentes no AllowedScopes; não mexe em schema,
        appsettings nem registro de DI.
      - **Evidência:** o validador check-public-api (que já validava o contrato V1 — tenant/scopes/hash/idempotência/registro)
        foi MESCLADO com novas asserções para os recursos + consistência escopo x AllowedScopes; **provado por mutação** (5
        mutações nas duas seções: escopo removido do AllowedScopes, filtro Budget virou Receipt, rota de detalhe quebrada,
        escopo inválido usado, e o token scope_required do contrato). Suíte C# inalterada **880 aprovados, 0 falhas, 19 ignorados**
        ; regressões verdes (public-api, api-scopes, api-security, api-idempotency, routes, razor-pages, authenticated-navigation,
        service-lifetimes, aspnet-scope, build-blockers, design-consistency, fiscal-flow, pwa, mobile-app).
      - **Limites conhecidos (não fechados aqui):** (a) só LEITURA — writes (quotes.write), webhooks.manage, files.read e
        analytics.read seguem sem endpoint (candidatos a incremento próprio); (b) webhooks ainda só listam o catálogo de
        eventos (/webhooks/events), sem entrega assinada; (c) sem filtros de busca/status por recurso (só paginação) —
        integradores paginam e filtram no cliente.

18. **Integração com contadores — exportação contábil das notas emitidas (item 12 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `3739bd1`.
       O audit mostrou a mesma situação do item 9 — fundação órfã pronta para ligar: o FiscalAccountingExportService JÁ existia
       (e já era unit-testado) gerando o pacote contábil em CSV/JSON (documento, cliente, data, serviço, bruto, retensões,
       líquido, status, origem, protocolo), e o escopo Fiscal.ExportAccounting estava declarado mas FORA do catálogo All e sem
       nenhum plano concedendo-o. O gap era preciso: não havia como o operador gerar e baixar o pacote pronto para o contador.
       Escopo fechado aqui: **exportação contábil das NFs emitidas**, reutilizando o serviço e o fluxo já existentes.
      - **Nada novo de infra:** reusa o FiscalAccountingExportService compartilhado (CSV/JSON) sem reimplementar; o mapeamento para
        FiscalDocumentSnapshot ficou num mapeador puro (FiscalAccountingSnapshotMapper) livre do DbContext, por isso unit-testável.
      - **Fiel ao registro manual:** só notas EMITIDAS da conta entram no pacote; número ausente vira "(sem número)" (nunca fabricado),
        retenção fica 0 (controle manual não registra retenção), status mapeia para ManualRegistered (controle manual, sem
        autorização de prefeitura) e provider/origem são preservados; o serviço vem da descrição do recebível vinculado.
      - **Competência no fuso comercial:** o filtro opcional desde/até compara o dia civil da emissão em America/Sao_Paulo
        (CommercialClock.NormalizeToUtc + ScheduleAgendaRules.BusinessToday), nunca no fuso do servidor. Download nomeado por período
        (fecho-contabil-AAAAMM.csv/json) e a página exibe a ressalva contábil (AccountingDisclaimer).
      - **Auditoria + gate:** cada export grava um evento FiscalAccountingExported (quantos documentos e período); protegido pela
        guarda da página (FinanceManage + EnsureAccountAccess); sem migração.
      - **Evidência:** novo validador check:accounting-export (complementa o check:fiscal-accounting-export genérico já existente),
        **provado por mutação** (5 mutações: status do filtro emitidas virou canceladas, data comercial removida, evento de auditoria
        renomeado, status do snapshot trocado e handler do formulário quebrado). Suíte C# **887 aprovados, 0 falhas, 19 ignorados**
        (7 testes novos do mapeador); regressões verdes (fiscal-flow, fiscal-control, no-fake-fiscal, razor-pages, design-consistency,
        routes, authenticated-navigation, public-api, api-scopes, api-security, api-idempotency, pwa, mobile-app).
      - **Limites conhecidos (não fechados aqui):** (a) o export é LEITURA (download) — não há integração push com ERP/provedor
        contábil externo (ex.: ContaAzul/OmniConta), que exigiria credenciais por conta e decisão R5; (b) retensões sempre 0 no
        controle manual; (c) o pacote se limita às notas fiscais emitidas (não inclui recebíveis/pagamentos como lançamentos).

19. **Backup exportável — exportação self-service de dados da conta (item 13 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `6bac1ea`.
       O audit mostrou a fundação mais completa até aqui: o DataExportService JÁ existia e fazia o trabalho pesado —
       criava o DataExportJob (expira em 24h), sanitizava segredos via ISensitiveDataSanitizer (a promessa da própria página:
       "nunca inclui senha, hash, token ou segredo"), auditava como Privacy.DataExported e devolvia um DataExportResult pronto.
       Estava registrada no DI mas SEM nenhum chamador; a permissão Privacy.ExportData já existia e estava no catálogo All;
       e o medidor UserUsage.BackupExports continuava órfão (nada o incrementava). Escopo fechado aqui: **ligar esse serviço
       a um download self-service na página de Privacidade**, mais incrementando o medidor órfão.
      - **Download self-service:** nova ação "Exportar meus dados" em /Settings/Privacy, gateada por Privacy.ExportData (+
        EnsureAccountAccess). Gera um bundle JSON estável (sem ciclos) e devolve o arquivo ao navegador; sem migração.
      - **Conteúdo do bundle:** clientes, serviços, documentos + itens, ordens de serviço, recibos, contratos recorrentes e
        notas fiscais da conta — tudo isolado por conta (AccountId + !IsDeleted), AsNoTracking, enums como nome.
      - **Financeiro só com permissão:** lançamentos/recebíveis entram no bundle APENAS se o usuário também tem permissão
        financeira (FinanceManage); a opção é um checkbox na página. Regra testada no mapeador puro AccountDataBackupShaper
        (inclusão condicional, tag de escopo core/full e contagem de registros).
      - **Medidor ligado:** novo UserUsageService.RegisterBackupExportAsync incrementa BackupExports (antes órfão) a cada export.
      - **Evidência:** novo validador check:data-export (complementa o check:privacy-compliance), **provado por mutação** (5
        mutações: serviço não chamado, medidor não chamado, tag de escopo rompida, handler do formulário quebrado e incremento
        do BackupExports alterado). Suíte C# **892 aprovados, 0 falhas, 19 ignorados** (5 testes novos); regressões verdes
        (privacy-compliance, backup-restore, razor-pages, design-consistency, routes, authenticated-navigation, public-api,
        accounting-export, fiscal-flow).
      - **Limites conhecidos (não fechados aqui):** (a) só EXPORT — importar/restaurar de volta (round-trip) fica adiado
        (requer remapeamento de IDs e tratamento de conflitos); (b) o bundle cobre registros de negócio, não anexos/arquivos
        nem configuração/modelos; (c) export síncrono (sem fila assíncrona) — contas muito grandes podem demorar.

20. **Relatórios de conversão — relatório de conversão comercial na central de inteligência (item 14 de `NEXT-RELEASE.md`) fechado nesta execução (2026-10-23):** commit `c02eaaa`.
       O audit mostrou que a família de relatórios já existia e era sólida: IntelligenceReportService com quatro relatórios,
       ReportPageModel base (filtros de período, export CSV, gate de plano) e o hub /Reports com tiles. O que faltava era o
       relatório de CONVERSÃO — o trajeto real do negócio: proposta aprovada -> ordem de serviço OU recibo -> dinheiro recebido.
       O modelo de dados já trazia a proveniência (status ConvertedToWorkOrder/Converted, ConvertedReceiptId/Number e
       ManualPayment.DocumentId). Escopo fechado aqui: **adicionar o relatório de conversão à família existente, sem duplicar a infraestrutura**.
      - **Métricas novas:** propostas aprovadas, valor aprovado, convertidas em OS, convertidas em recibo, valor convertido,
        taxa de conversão, valor recebido das aprovadas e tempo médio decisão->conversão; -1 vira "Dados insuficientes" na view.
      - **Linhas por cliente:** aprovações, valor aprovado, convertido, recebido e taxa individual, ordenado por valor convertido;
        export CSV herdado da base (prefixo conversao-comercial).
      - **Regras puras testadas:** novo ConversionReportRules (união OS/recibo, guarda de divisão, tempo médio com fallbacks e
        clamp não negativo) — 17 testes novos, herméticos, sem DbContext.
      - **Gate e isolamento:** mesmo gate de plano do funil (CommercialMetricsEnabled); consultas isoladas por conta + !IsDeleted;
        recebimentos apenas ativos e pagos no período, atribuídos por documento.
      - **Hub:** tile "Conversão comercial" na central de inteligência com a taxa ao vivo.
      - **Evidência:** novo validador check:conversion-report, **provado por mutação** (5 mutações: divisor da taxa, união da
        conversão virou interseção, status do recebimento trocado, prefixo CSV quebrado e tile do hub removido). Suíte C#
        **909 aprovados, 0 falhas, 19 ignorados**; regressões verdes (executive-reports, bi-reports, no-fake-analytics, routes,
        razor-pages, design-consistency, authenticated-navigation, screen-quality).
      - **Limites conhecidos (não fechados aqui):** (a) conversão medida sobre orçamentos aprovados criados no período (mesma
        semântica dos demais relatórios); (b) "tempo até converter" usa UpdatedAt como proxy de etapa quando houve várias
        transições; (c) sem comparação entre períodos (mês vs mês) — o _Report compartilhado ainda é de período único.
