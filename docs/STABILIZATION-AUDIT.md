# Auditoria de estabilização

Inventário baseado em rotas, PageModels, entidades, configurações e testes existentes em 28/07/2026. “Funcional” exige fluxo integrado; arquivo ou entidade vazia não foi contado como entrega.

| Módulo | Arquivos/rota | Domínio e persistência | Interface/regra/autorização/feedback | Testes | Status | Pendência |
|---|---|---|---|---|---|---|
| Landing | `Pages/Index.cshtml`, `/` | sem gravação | pública, CTAs | contraste estático | Parcial | inspeção visual real |
| Cadastro PF/PJ | `Auth/Register.*`, `/Auth/Register`; `AuthService` | usuário, conta, owner, cobrança, FREE, issuer, notificação e auditoria; transação única | anônimo, validação e correlationId | sem integração PostgreSQL | Parcial | executar E2E PF/PJ |
| Login | `Auth/Login.*`, `/Auth/Login` | usuário e sessão | anônimo, cookie | não localizado | Parcial | teste de cookie/sessão |
| Recuperação de senha | não localizada | não localizado | não integrada | nenhum | Não integrado | definir fluxo seguro |
| Onboarding | `Onboarding/Index.*`, `/Onboarding` | perfil | autenticado | não localizado | Parcial | smoke após cadastro |
| Dashboard | `Dashboard/Index.*`, `/Dashboard` | queries | autenticado | não localizado | Parcial | auditoria visual/textos |
| Perfil do emitente | `Profile/Index.*` | `IssuerProfile`, `ProfileService` | autenticado | não localizado | Parcial | isolamento por conta |
| Clientes | `Clients/*` | `Client` | CRUD autenticado | não localizado | Parcial | limites e exclusão lógica E2E |
| Serviços | `Services/Index.cshtml` | não localizado | página sem PageModel | nenhum | Placeholder | integrar persistência |
| Modelos | `Templates/*` | templates e itens | leitura/uso | não localizado | Parcial | validar criação real |
| Orçamento/recibo/PDF | `Documents/*`, serviços e QuestPDF | documentos, itens, snapshots | autenticado | testes unitários parciais | Parcial | E2E, autorização e PDF |
| Histórico | `Historico.cshtml` | documentos | interface legada | nenhum | Parcial | consolidar rota |
| Aprovação pública | `PublicQuotes/*` | `PublicQuote` | token público | testes unitários parciais | Parcial | teste navegador/expiração |
| Planos/assinatura | `Subscription/*`, catálogo | assinatura e versões | autenticado | `CommercialPlatformTests` | Parcial | E2E PostgreSQL/readiness |
| Pagamentos | entidades/gateway Mercado Pago | pagamentos/eventos | configuração desabilitável | testes parciais | Parcial | sandbox é etapa posterior |
| Notificações | `Notifications/*` | `Notification` | autenticado/toast | não localizado | Parcial | testar persistência/ARIA |
| Suporte | `Support/Index.cshtml`, `/Support` | sem solicitação persistida | público | nenhum | Placeholder | redesign e módulo de chamados |
| Termos | `Termos.cshtml`, `/Termos` | timestamp legado no usuário | público | nenhum | Parcial | versionamento legal e revisão jurídica |
| Privacidade | `Privacidade.cshtml`, `/Privacidade` | timestamp legado | público | nenhum | Parcial | documento/versionamento completo |
| Cookies | rota não localizada | cookie de autenticação | não integrada | nenhum | Não integrado | inventário operacional |
| SuperAdministrador | `Admin/Index.cshtml` e serviços | contas/usuários | policy administrativa | testes parciais | Parcial | diagnóstico protegido |
| Configurações/auditoria | entidades e serviços | `AdminSetting`, `AuditLog` | administrativa | não localizado | Parcial | telas/policies/justificativas |
| Health checks | `Health/PostgresHealthCheck.cs` | PostgreSQL | endpoints no `Program` | não localizado | Parcial | FREE publicado deve afetar readiness |

## Bloqueadores tratados nesta alteração

1. Desalinhamento determinístico entre migrations, script e o modelo usado no cadastro.
2. Ausência de fronteira transacional explícita no cadastro.
3. Falta de telemetria por etapa e classificação segura dos SqlStates.

Os itens restantes continuam explicitamente classificados; não foram declarados concluídos sem comprovação.
