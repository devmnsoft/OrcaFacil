# Auditoria da jornada comercial — Release Operacional 6

Data da auditoria: 28/07/2026. Escopo: estado do `main` incorporado pela branch de trabalho antes da evolução comercial.

## Portão da base

| Área | Evidência encontrada | Situação / risco |
|---|---|---|
| Dashboard | `/Dashboard/Index` é a implementação autenticada; ainda existe a página legada `/Dashboard` | Funcional, mas a página legada deve permanecer apenas como redirecionamento para evitar duas experiências. |
| Cadastro PF/PJ e sessão | `AuthService`, `Register`, `AccountMember`, `BusinessAccount` e validação de `session_version` no cookie | Implementado; exige PostgreSQL para homologação ponta a ponta. |
| Login/logout/recuperação | Razor Pages em `Pages/Auth`, token de redefinição protegido e `EmailOutboxWorker` | Implementado e coberto pela release anterior; entrega SMTP depende de configuração externa. |
| E-mail | `EmailOutboxMessage`, Gmail SMTP e worker único | Reutilizar obrigatoriamente; não criar outra fila. |
| Layouts, onboarding e templates | layouts público/autenticado, onboarding e `BudgetTemplate` | Estrutura real existente; pré-visualização/PDF usa QuestPDF. |
| Build/testes | solução .NET e verificadores Node existentes | Revalidar após a migration aditiva. |

## Inventário por etapa

| Etapa | Entidade / tabela | Serviço | Rota / PageModel | Status e permissão | Regra de plano | Real / placeholder | Risco e evolução necessária |
|---|---|---|---|---|---|---|---|
| Cliente | `Client` / `clients` | acesso EF nas Pages | `/Clients/*` | autenticado, isolado por conta | limites via `IPlanAccessService` | Real | ampliar detalhes com projeção comercial, sempre filtrada por `AccountId`. |
| Serviço | catálogo no fluxo web | PageModel atual | `/Services` | autenticado | limite de serviços | Parcial | consolidar entidade persistida antes de relacionar itens. |
| Orçamento e itens | `Document`, `DocumentItem` / `documents`, `document_items` | `DocumentService`, `DocumentQueries` | `/Documents/*` | string de status; ações do usuário autenticado | limites de documento/PDF | Real, estado frágil | centralizar transições; impedir PageModel de escrever status. |
| Templates/PDF | `BudgetTemplate`; snapshot ainda ausente | `QuestPdfDocumentService` | `/Templates/*`, `/Documents/Pdf/{id}` | conta autenticada | features de template/marca | Real | congelar template, branding e conteúdo por revisão. |
| Envio/link público | `PublicQuote` / `public_quotes` guarda token puro | `DocumentService.GeneratePublicLinkAsync` | `/PublicQuotes/View`, API `/publicquotes` | token bearer | `sharing.public_link`, aprovação | Real, inseguro para nova jornada | substituir gradualmente por `PublicDocumentAccess`, token aleatório com somente hash persistido. |
| Visualização | contador em `PublicQuote` | lógica direta no PageModel | GET público | sem deduplicação | não aplicada | Real, insuficiente | deduplicar por janela e HMAC de IP/UA; não atribuir identidade. |
| Decisão | campos mutáveis em `PublicQuote` e `Document` | `DocumentService.ApproveAsync` | POST público | aprovação única não garantida no banco | `public_approval.enabled` | Aprovação parcial | usar decisão imutável, idempotency key, transação e índice único; incluir recusa/alteração. |
| Notificações/timeline | `Notification`, `ActivityEvent`, `AuditLog`; `EmailOutboxMessage` | `NotificationService`, `AuditService` | `/Notifications` | autenticado e conta | features de e-mail | Real, não unificado | projetar linguagem humana e chaves idempotentes; nunca mostrar auditoria bruta. |
| Versionamento | `DocumentRevision` / `document_revisions` (adicionado nesta evolução) | transição central; orquestração pendente | `/Documents/{id}/Versions` ainda não existia | conta + permissão de documento | `quote_versioning.enabled` | Persistência criada | implementar snapshot protegido, comparação e criação automática ao editar/enviar. |
| Acompanhamento | `CommercialFollowUp` / `commercial_follow_ups` (adicionado) | ainda sem orquestrador | ação e configuração ainda não existiam | conta, editor | manual/automático | Persistência criada | tela, configuração conservadora e worker idempotente ainda necessários. |
| Ordem/agenda | `WorkOrder` / `work_orders` (adicionado) | máquina de estado adicionada | rotas ainda não existiam | conta + papéis operacionais | `work_orders.enabled`, `schedule.enabled` | Domínio/persistência | criar conversão transacional, páginas e agenda; índice único impede ordem duplicada por revisão. |
| Recibo | `Document` do tipo receipt | conversão direta orçamento→recibo | criação/PDF existentes | autenticado | `document.convert_to_receipt` | Real, fluxo antigo | vincular à OS concluída e exigir pagamento manual, sem presumir confirmação bancária. |
| Dashboard/funil | `DashboardQueries` | Dapper/queries | `/Dashboard/Index`; pipeline ausente | conta | features comerciais legadas | Dashboard real, funil ausente | criar projeções reais e atenção sem alertas inventados. |

## Decisões de arquitetura desta evolução

1. `Document` continua agregado comercial original; não foi criada uma segunda entidade de orçamento.
2. `PublicQuote` permanece somente para compatibilidade de links antigos. Novos contratos usam revisão, acesso com hash e decisão imutável.
3. Revisão, acesso, decisão, acompanhamento e ordem carregam `AccountId`; índices compostos fazem isolamento e idempotência também no banco.
4. Estados terminais não possuem saída implícita. Reativação/reabertura futura deve ser uma operação auditada explícita, não uma transição comum.
5. Snapshots são preservados como conteúdo protegido na aplicação; o banco não deve receber token puro nem telemetria técnica em claro.

## Pendências reais

Esta auditoria não declara a jornada completa pronta. Ainda são necessários os orquestradores transacionais, portal premium na nova rota, comparação, páginas de OS/agenda/pipeline, worker, notificações/e-mails, recibo a partir do pagamento e testes PostgreSQL/Playwright completos antes do aceite operacional.
