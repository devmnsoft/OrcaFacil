# Auditoria de implementação — do orçamento ao recebimento

**Base auditada:** `main` após o PR #76 (`6f8313a`). **Entrega:** Release Operacional 8.

## Resultado executivo

A base já possuía autenticação por cookie e `SessionVersion`, isolamento de conta, planos, documentos, PDF, outbox, notificações e a migration inicial da jornada comercial. A Release 8 consolida essas fundações em um limite transacional (`ICommercialJourneyService`), mantém o token público apenas como hash, adiciona pagamentos manuais e recibos operacionais e deixa explícita a diferença entre registrar um pagamento e obter confirmação bancária.

| Estrutura | Funcionava antes | Lacuna/fundação encontrada | Rota/PageModel e autorização | Migration/teste | Melhoria aplicada |
|---|---|---|---|---|---|
| `Document` | Criação, itens, totais, PDF e decisão legada | Status textual e fluxo distribuído | `/Documents/*`; usuário autenticado e conta | migrations iniciais; `DomainTests` | Orquestração passa pela máquina de estados e sempre filtra `AccountId` |
| `DocumentItem` | Quantidade, preço e desconto | Aceitava cálculo defensivo sem política única | builder de orçamento | tabela `document_items`; `CommercialCalculatorTests` | cálculo servidor decimal, arredondamento `AwayFromZero` e rejeição de valores inválidos |
| `DocumentRevision` | Entidade/configuração e snapshot protegido | Era fundação sem fluxo operacional | `/Documents/Versions/{id}` | `AddCommercialJourney`; testes de schema/snapshot | criação serializável, versão corrente e índice único `(AccountId, DocumentId, VersionNumber)` |
| `DocumentStatus` / transition service | Máquina completa | não era usada por toda a jornada | serviços autenticados | `CommercialStatusTransitionTests` | transições centralizadas; PageModels não devem atribuir status |
| `DocumentSnapshotSerializer` | JSON canônico e SHA-256 | não conectado ao envio | serviço central | `DocumentSnapshotSerializerTests` | revisão contém cópia congelada de emitente, cliente, itens, valores e apresentação |
| `PublicDocumentAccess` / token | token criptográfico, hash e expiração modelados | portal legado ainda usava `PublicQuote.Token` puro | `/p/orcamento/{token}` anônimo, rate limit | `AddCommercialJourney`; `PublicDocumentTokenServiceTests` | fluxo novo consulta SHA-256 em tempo constante, valida expiração/revogação/versão |
| `PublicDocumentDecision` | entidade e restrições únicas | sem orquestração pública completa | portal público; chave idempotente | índices únicos por revisão e idempotência | aprovação, recusa e alteração são mutuamente exclusivas e transacionais; comentários limitados |
| `CommercialFollowUp` | persistência e índice temporal | somente fundação | timeline de documento | `AddCommercialJourney` | timeline humanizada usa `ActivityEvent`, sem expor auditoria bruta |
| `WorkOrder` / transition service | modelo congelado e estados | sem conversão e handlers operacionais | `/WorkOrders/*`; conta + plano | `AddCommercialJourney`; testes de transição | conversão explícita e idempotente, agenda, início e conclusão separados |
| `Payment` | cobrança de assinatura via gateway | não representa recebimento de serviço | administrativo/assinatura | migrations de billing | preservado sem mistura de conceitos |
| `ManualPayment` | inexistente | — | `/Payments/Register/{id}`; conta | `AddManualPaymentsAndReceipts`; testes de contrato | registro manual positivo, idempotente, ligado à ordem/documento/cliente |
| `Receipt` | recibo-documento legado | não ligado a pagamento operacional | `/Receipts/Details/{id}` | `AddManualPaymentsAndReceipts` | recibo somente após pagamento, snapshots, valor por extenso e aviso fiscal |
| `QuestPdfDocumentService` | PDF QuestPDF de orçamento/recibo | composição ainda merece refatoração visual adicional | `/Documents/Pdf/{id}` | testes PDF existentes | revisão congelada é a fonte prevista para preview/portal |
| `EmailOutboxMessage` | worker, retry, idempotência e payload protegido | templates comerciais incompletos | background worker | migrations/outbox tests | serviço central reutiliza a fila existente; não foi criada fila paralela |
| `Notification` | central, leitura e ações | eventos comerciais parciais | `/Notifications` | migration/testes existentes | decisões/eventos têm fonte única para notificações deduplicadas |
| `ActivityEvent` | auditoria humanizável por conta | cobertura comercial parcial | timeline/cliente 360 | schema existente | eventos de versão, envio, decisão, ordem, agenda, execução, pagamento e recibo |
| `AuditLog` | trilha técnica | não apropriada para cliente | administrativo | migrations existentes | permanece separada da timeline humanizada |
| `IPlanAccessService` | plano efetivo, fallback grátis e limites | features comerciais não estavam em todos os handlers | cada mutação comercial | `PlanAccessServiceTests` | `public_link.enabled` e `work_orders.enabled` validados no backend, sem `if` por nome de plano |

## Controles transversais

- **Conta:** identificador vem de `ICurrentAccountService`; nenhum `AccountId` do request autoriza acesso.
- **Concorrência:** transação `Serializable`, row-version PostgreSQL e índices únicos protegem revisão, decisão, ordem, pagamento e recibo.
- **Idempotência:** decisões e pagamentos persistem chave única; ordem e recibo são únicos por origem.
- **Privacidade:** banco recebe apenas hash do token; IP e User-Agent também são resumidos por SHA-256; logs não recebem token ou documento pessoal.
- **Preservação:** pausa ou retorno ao plano grátis não remove documentos, revisões, links, ordens, pagamentos nem recibos; apenas novas mutações consultam o plano efetivo.

## Pendências reais verificáveis

O ambiente local desta execução não contém o SDK .NET 10 nem uma instância PostgreSQL configurada. Portanto build, aplicação da migration, readiness, Playwright e screenshots dependem do workflow CI com os segredos/serviços apropriados. Não se declara esses itens como aprovados sem evidência do runner.
