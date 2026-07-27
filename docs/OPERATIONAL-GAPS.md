# Lacunas operacionais após o PR 56

Revisão realizada em 27/07/2026 sobre o commit `0b805be`. Este documento separa o que existe no repositório do que está efetivamente operacional; presença de entidade ou página não é tratada como funcionalidade concluída.

## Inventário existente

- O domínio já contém `BusinessAccount`, `AccountMember`, catálogo versionado (`Plan`, `PlanVersion`, `Feature`, `PlanFeatureValue`), assinatura, fatura, pagamento, eventos, override, atividade e sessão de suporte.
- `OrcaFacilDbContext` expõe os conjuntos dessas entidades e usa o schema `orcafacil`. Há três migrations: criação inicial, consolidação de schema e isolamento por conta.
- Cadastro cria usuário, conta, Owner, perfil de cobrança, assinatura, emitente e notificação. Nesta revisão a assinatura nova passou a exigir a versão FREE publicada vigente.
- `PlanAccessService` e `EfPlanAccessDataSource` consultam versões e benefícios por `AccountId`. Clientes, documentos e parte dos PageModels, porém, ainda autorizam por `UserId`.
- Existem páginas Razor reais para autenticação, clientes, documentos, notificações, perfil e dashboard do cliente. Há páginas de assinatura e perfil de cobrança, mas a página principal de assinatura é predominantemente estática.
- A área Admin possui dashboard com PageModel e consultas. As páginas de clientes, usuários, planos e pagamentos são em grande parte protótipos sem PageModel, handlers POST ou persistência.

## Handlers funcionais e páginas decorativas

Os CRUDs de clientes e documentos possuem handlers e acesso ao banco, embora ainda usem o usuário como fronteira de autorização. Login e cadastro chamam `AuthService`. O diagnóstico de banco e dashboard administrativo consultam dados reais parcialmente.

São decorativas ou incompletas: Admin/Clients, Admin/Plans, Admin/Payments, Admin/Users/Index e Admin/Users/Details. Botões críticos nessas páginas não são formulários POST e não executam serviços administrativos. Não existem rotas Admin/Accounts, detalhes de conta, editor/comparador de versões, features, cobranças, auditoria, atividade e suporte. Não existe página de seleção para usuários com múltiplas contas.

## Stubs e duplicações

- `PlanAccessService.InvalidateAccountCacheAsync` retorna `Task.CompletedTask`, sem cache para invalidar.
- `AuditService` e `LoggerService` retornam `Task.CompletedTask` depois de anexar entidades ao contexto; dependem de outro chamador salvar a unidade de trabalho.
- `PlanLimitService`, `PlanEntitlementService` e o acesso versionado coexistem. Os fluxos antigos ainda usam os dois primeiros e `UserUsageService`, criando duas fontes de decisão de plano.
- Não existem `ISubscriptionLifecycleService`, serviços administrativos especializados, serviço de faturas nem serviço/worker de reconciliação.

## Isolamento e dados legados

- A busca encontrou 23 filtros explícitos por `UserId` nas camadas Web/Application/Persistence. Clientes, documentos, PDF e modelos privados ainda contêm decisões dessa forma.
- O endpoint de PDF em `Program.cs` autoriza por `Document.UserId` e escolhe marca d'água pelo claim legado `plan`.
- `SuperAdminDashboardQueries` conta usuários por `UserAccount.Plan`; portanto os cards de plano não refletem contas ou versões efetivas.
- `PlanCatalogDefinitions` contém preços fixos. Esses valores devem ficar apenas no seed; apresentação e faturamento ainda precisam consultar `PlanVersion`.
- `services.active_limit` é inferido por `ActivityEvent`; não existe a entidade nem CRUD `ServiceCatalogItem`.
- `Document.ClientId` existe na fundação, mas criação e visão ainda têm consultas por usuário e fallback por dados textuais.

## Cobrança e segurança

- `BillingInvoice`, `Payment`, `PaymentEvent` e evento de webhook existem, mas não formam ainda um ciclo transacional consolidado.
- O endpoint Mercado Pago grava o corpo bruto, chama o gateway em linha e responde somente após persistir. É necessário sanitizar, autenticar antes de armazenar, deduplicar atomicamente e processar/reconciliar consultando o provedor.
- Pix e boleto não possuem fluxo Razor completo nem validação integral do pagador. Não há reconciliação com advisory lock.
- `SupportAccessSession` não possui fluxo funcional.
- O health check público de banco expõe detalhes internos e readiness não valida plano FREE, roles, seeder, credenciais habilitadas ou migrations pendentes.

## Migrations, SQL, build e rotas

- A migration `ConsolidateAccountIsolation` introduz a fundação comercial, mas faltam migrations para catálogo de serviços, chave idempotente de notificações e backfills operacionais solicitados.
- `database/script_completop.sql` deve ser reconciliado com qualquer nova migration antes de produção; não foi aplicado a uma instância PostgreSQL nesta revisão.
- O container não possui `dotnet`; portanto clean, restore, build, testes e validação de migrations estão bloqueados pelo ambiente. A tentativa de baixar o SDK 10 também recebeu HTTP 403 do proxy.
- Também estão ausentes rotas de escolha de plano, seleção de conta, gestão administrativa de contas, versões/benefícios, faturas, suporte e auditoria.

## Prioridade recomendada para próximas entregas

1. Disponibilizar SDK 10 e tornar build/testes verdes antes de ampliar a alteração.
2. Concluir seleção de múltiplas contas e migrar todos os filtros de autorização para `AccountId`.
3. Criar lifecycle único, backfill idempotente e catálogo real de serviços.
4. Implementar fatura, um único fluxo Mercado Pago, webhook autenticado e reconciliação.
5. Só então conectar os PageModels administrativos aos serviços transacionais e completar a interface.
