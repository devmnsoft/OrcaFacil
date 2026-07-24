# Routes Matrix — OrçaFácil

| Rota | Arquivo | Autenticação exigida | Perfil autorizado | Status HTTP esperado | Funcionalidade | Dependências | Estado |
|---|---|---:|---|---|---|---|---|
| `/` | `Pages/Index.cshtml` | Não | Público | 200 | Landing | Razor/static | Parcial |
| `/ComoFunciona` | `Pages/ComoFunciona.cshtml` | Não | Público | 200 | Explicação | Razor | Parcial |
| `/Auth/Register` | `Pages/Auth/Register.cshtml*` | Não | Público | 200/302 | Cadastro | AuthService, DbContext | Parcial |
| `/Auth/Login` | `Pages/Auth/Login.cshtml*` | Não | Público | 200/302 | Login | AuthService, cookie | Parcial |
| `/Auth/Logout` | `Pages/Auth/Logout.cshtml.cs` | Sim | Usuário | 302 | Logout | Cookie auth | Parcial |
| `/Dashboard` | `Pages/Dashboard.cshtml` | Sim esperado | Usuário | 200/302 | Dashboard legado | Layout/auth | Duplicado |
| `/Dashboard/Index` | `Pages/Dashboard/Index.cshtml*` | Sim | Usuário | 200 | Dashboard | DashboardQueries | Parcial |
| `/Profile` | `Pages/Profile/Index.cshtml*` | Sim | Usuário | 200 | Perfil emitente | ProfileService | Parcial |
| `/Clients` | `Pages/Clients/Index.cshtml*` | Sim | Usuário | 200 | Listar clientes | DbContext | Parcial |
| `/Clients/Create` | `Pages/Clients/Create.cshtml*` | Sim | Usuário | 200/302 | Criar cliente | DbContext | Parcial |
| `/Clients/Edit` | `Pages/Clients/Edit.cshtml*` | Sim | Usuário | 200/302/404 | Editar cliente | DbContext | Parcial |
| `/Clients/Details` | `Pages/Clients/Details.cshtml*` | Sim | Usuário | 200/404 | Detalhar cliente | DbContext | Parcial |
| `/Services` | `Pages/Services/Index.cshtml` | Sim | Usuário | 200 | Empty state de serviços | Templates | Stub |
| `/Templates` | `Pages/Templates/Index.cshtml*` | Sim | Usuário | 200 | Modelos | BudgetTemplate | Parcial |
| `/Documents` | `Pages/Documents/Index.cshtml*` | Sim | Usuário | 200 | Histórico | DocumentQueries | Parcial |
| `/Documents/CreateBudget` | `Pages/Documents/CreateBudget.cshtml*` | Sim | Usuário | 200/302 | Novo orçamento | DocumentService | Parcial |
| `/Documents/CreateReceipt` | `Pages/Documents/CreateReceipt.cshtml*` | Sim | Usuário | 200/302 | Novo recibo | DocumentService, PDF | Parcial |
| `/Documents/Details` | `Pages/Documents/Details.cshtml*` | Sim | Usuário | 200/404 | Detalhes | DocumentQueries | Parcial |
| `/Documents/Pdf/{id}` | `Program.cs` endpoint minimal | Sim | Dono do documento | 200/404 | PDF | IPdfService, DbContext | Parcial |
| `/Subscription` | `Pages/Subscription/Index.cshtml` | Sim | Usuário | 200 | Assinatura | Billing/Plans | Parcial |
| `/Subscription/BillingProfile` | `Pages/Subscription/BillingProfile.cshtml*` | Sim | Usuário | 200/302 | Perfil cobrança | Billing | Parcial |
| `/Notifications` | `Pages/Notifications/Index.cshtml*` | Sim | Usuário | 200 | Notificações | NotificationService | Parcial |
| `/Activity` | Não encontrado | Sim esperado | Usuário | 404 | Atividade/auditoria usuário | N/A | Ausente |
| `/Support` | `Pages/Support/Index.cshtml` | Sim | Usuário | 200 | Ajuda | Razor | Parcial |
| `/p/{token}` | `Pages/PublicQuotes/View.cshtml*` | Não | Público com token | 200/404/410 | Aprovação pública | PublicQuote | Parcial |
| `/diagnostico` | `Pages/Diagnostico.cshtml*` | Sim | SuperAdmin | 200/302/403 | Diagnóstico | Policy SuperAdmin | Implementado |
| `/Admin/Dashboard` | `Areas/Admin/Pages/Dashboard.cshtml*` | Sim | SuperAdmin | 200/302/403 | Admin dashboard | SuperAdminDashboardQueries | Parcial |
| `/Admin/Users` | `Areas/Admin/Pages/Users/Index.cshtml` e legado `Users.cshtml` | Sim | SuperAdmin | 200/302/403 | Usuários | AdminService | Duplicado |
| `/Admin/Clients` | `Areas/Admin/Pages/Clients/Index.cshtml` | Sim | SuperAdmin | 200/302/403 | Clientes admin | AdminService | Parcial |
| `/Admin/Payments` | `Areas/Admin/Pages/Payments/Index.cshtml` | Sim | SuperAdmin | 200/302/403 | Pagamentos | Payments | Parcial |
| `/Admin/Plans` | `Areas/Admin/Pages/Plans/Index.cshtml` | Sim | SuperAdmin | 200/302/403 | Planos | Plan services | Parcial |
| `/Admin/Audit` | Não encontrado; há `Logs.cshtml` | Sim | SuperAdmin | 404 | Auditoria admin | AuditLog | Ausente |
| `/Admin/Settings/Database` | `Areas/Admin/Pages/Settings/Database.cshtml*` | Sim | SuperAdmin | 200/302/403 | Diagnóstico DB | DatabaseDiagnosticsService | Parcial |
| `/health` | `Program.cs` | Não | Público | 200/503 | Health básico | HealthChecks | Parcial |
| `/health/db` | `Program.cs` | Não | Público | 200/503 | Health DB | DatabaseDiagnosticsService | Parcial |
| `/health/version` | `Program.cs` | Não | Público | 200 | Versão | Environment | Parcial |

## Achados de rotas

- Existem rotas legadas duplicadas (`/Login`, `/Cadastro`, `/Dashboard`, `/Historico`, `/Emitente`) coexistindo com a estrutura nova em subpastas.
- `/diagnostico` possui `[Authorize(Policy = "SuperAdmin")]` no PageModel e não deve ficar público.
- Menus principais apontam para páginas existentes; `/Activity` e `/Admin/Audit` estão ausentes e devem ser implementadas ou removidas de qualquer menu futuro.
