# Auditoria de rotas — Release Operacional 5.1

Auditoria realizada sobre as diretivas `@page`, os endpoints mínimos e os controllers. A chave de concorrência usada é padrão normalizado + método HTTP + ordem. Rotas de `Index.cshtml` são tratadas pelo Razor Pages com a URL curta canônica.

| Rota canônica | Endpoint / arquivo | Autorização | Estado | Duplicidade e correção |
|---|---|---|---|---|
| `/Dashboard` | `/Dashboard/Index` — `Pages/Dashboard/Index.cshtml` | autenticado | corrigido | `Pages/Dashboard.cshtml` concorria com o Index e foi removido. |
| `/Support` | `/Support/Index` — `Pages/Support/Index.cshtml` | público | válido | nenhuma. |
| `/Precos` | `/Precos` — `Pages/Precos.cshtml` | público | válido | nenhuma. |
| `/Notifications` | `/Notifications/Index` — `Pages/Notifications/Index.cshtml` | autenticado | válido | nenhuma. |
| `/Profile` | `/Profile/Index` — `Pages/Profile/Index.cshtml` | autenticado | válido | nenhuma. |
| `/Subscription` | `/Subscription/Index` — `Pages/Subscription/Index.cshtml` | autenticado | válido | nenhuma. |
| `/Documents` | `/Documents/Index` — `Pages/Documents/Index.cshtml` | autenticado | válido | o endpoint mínimo `/Documents/Pdf/{id:guid}` é distinto. |
| `/Templates` | `/Templates/Index` — `Pages/Templates/Index.cshtml` | autenticado | válido | nenhuma. |
| `/Services` | `/Services/Index` — `Pages/Services/Index.cshtml` | autenticado | válido | nenhuma. |
| `/Clients` | `/Clients/Index` — `Pages/Clients/Index.cshtml` | autenticado | válido | rotas Create/Edit/Details são distintas. |
| `/Onboarding` | `/Onboarding/Index` — `Pages/Onboarding/Index.cshtml` | autenticado | válido | nenhuma. |
| `/Admin` | `/Admin/Index` — `Pages/Admin/Index.cshtml` | política administrativa | válido | não concorre com controllers `/api/admin`. |

## Causa raiz e prevenção

O placeholder `Pages/Dashboard.cshtml` e `Pages/Dashboard/Index.cshtml` produziam o mesmo caminho convencional `/Dashboard`. O roteador encontrava dois candidatos igualmente válidos e lançava `AmbiguousMatchException`, resultando em HTTP 500. A correção remove o placeholder, sem alias ou convenção substituta.

`RazorRouteUniquenessTests` inicializa a aplicação com `WebApplicationFactory`, lê todos os `EndpointDataSource`, normaliza os padrões e falha diante de destinos incompatíveis com a mesma rota, método e ordem. Há ainda uma asserção dedicada a `/Dashboard` e uma matriz HTTP das rotas principais.
