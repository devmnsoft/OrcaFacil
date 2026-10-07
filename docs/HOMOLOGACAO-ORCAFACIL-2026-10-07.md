# Homologacao parcial do template e preparacao de producao

Data: 2026-10-07  
Baseline reconfirmado: `49cdf231460d214d27d6c19591eb38c59a61723b`  
Parecer: nao apto para producao sem nova rodada de homologacao funcional, banco, PDF, cobranca, seguranca e PWA em navegador autenticado.

## Resumo das correcoes executadas

- Corrigido o preparo dos testes de `BudgetSuggestionApplyTests`: o `Scenario` agora devolve tambem o repositorio de `DocumentItem`, preservando as verificacoes de item, quantidade e vinculo com o documento.
- Organizado o entry point CSS: `app.css` permanece como compatibilidade e passa a importar `pipeline.css`; estilos especificos de pipeline foram extraidos para `wwwroot/css/pipeline.css`, preservando a regra legada `.of-pipeline`.
- Reforcados tokens e escopos de contraste para superficies escuras: labels, textos auxiliares, links, controles, placeholder e campos mantem pares explicitos de fundo/texto.
- Reforcado o menu movel autenticado: painel fechado recebe `aria-hidden` e `inert` quando suportado; em navegadores sem `inert`, os elementos focaveis recebem `tabindex="-1"` ate a reabertura; Escape e fechamento retornam foco ao botao de abertura.
- Atualizado o cache publico do PWA para incluir os modulos CSS importados por `app.css`, sem incluir rotas autenticadas ou respostas privadas.

## Evidencias executadas

| Area | Cenario | Resultado | Evidencia |
| --- | --- | --- | --- |
| Baseline | `git rev-parse HEAD` | executado com sucesso | `49cdf231460d214d27d6c19591eb38c59a61723b` |
| Build | `dotnet build OrcaFacil.sln --no-restore` | executado com sucesso | 0 erros, 11 avisos |
| Testes | `dotnet test OrcaFacil.sln --no-restore --no-build` | executado com sucesso | 568 aprovados, 6 ignorados |
| Testes IA/orcamento | `BudgetSuggestionApplyTests` | executado com sucesso | build/testes passam apos ajuste do `Scenario` |
| Contraste | Pares alterados em tokens | executado com sucesso | 16.08:1, 13.42:1, 10.15:1, 15.68:1 e 4.96:1 |
| CSS | Separacao pipeline/app | executado com sucesso | `app.css` importa `pipeline.css`; regra `.of-pipeline` preservada |
| PWA | Inspecao estatica do service worker | parcial | cache limitado a assets publicos; modulos CSS importados incluidos; rotas sensiveis excluidas |
| Navegador autenticado | Computed styles, teclado, screenshots | nao executado | requer ambiente autenticado e massa de dados |
| Banco/PostgreSQL | Integracao real multi-conta | nao executado | testes PostgreSQL estao ignorados nesta execucao |
| PDF final | Renderizacao e impressao | nao executado | sem geracao/renderizacao de PDFs nesta rodada |
| Pagamento | Checkout/webhook/estorno | nao executado | sem sandbox de pagamento nesta rodada |

## Avisos e riscos observados

- Pacotes com vulnerabilidades conhecidas: `Microsoft.OpenApi` alta severidade; `MailKit` e `MimeKit` moderadas.
- Conflito de versao em `Microsoft.EntityFrameworkCore.Relational`: projetos resolvem entre `10.0.4` e `10.0.10`.
- Testes PostgreSQL de idempotencia, concorrencia, tenant e recebimentos continuam ignorados; portanto essas jornadas nao estao homologadas.
- Alteracoes em `bin/obj/.vs` ja existiam no worktree e nao foram tratadas como codigo-fonte.

## Inventario das rotas visiveis no menu autenticado

| Grupo | Rota | Tela | Permissao | Status |
| --- | --- | --- | --- | --- |
| Principal | `/Dashboard/Index` | Dashboard | padrao autenticado | parcial |
| Principal | `/Search/Index` | Busca global | `Search.Global` | nao executado |
| Principal | `/CommandCenter/Index` | Command Center | `CommandCenter.Use` | nao executado |
| Principal | `/Assistant/Index` | Assistente interno | `Assistant.Use` | nao executado |
| Comercial | `/CommercialRoutine/Index` | Rotina comercial | padrao autenticado | nao executado |
| Comercial | `/Documents/New` | Novo orcamento | `documents.create` | parcial |
| Comercial | `/Documents/BudgetAssistant` | Assistente de orcamento | `documents.create` | parcial |
| Comercial | `/Documents/Index` | Orcamentos | `documents.read` | nao executado |
| Comercial | `/Clients/Index` | Clientes | `clients.read` | nao executado |
| Comercial | `/MessageTemplates/Index` | Templates de mensagem | padrao autenticado | nao executado |
| Operacao | `/WorkOrders/Index` | Ordens de servico | padrao autenticado | nao executado |
| Operacao | `/Schedule/Index` | Agenda | padrao autenticado | nao executado |
| Operacao | `/Contracts/Index` | Contratos | padrao autenticado | nao executado |
| Financeiro | `/Payments/Index` | Pagamentos | padrao autenticado | nao executado |
| Financeiro | `/Receipts/Index` | Recibos | `receipts.read` | nao executado |
| Financeiro | `/Subscription/Index` | Meu plano | padrao autenticado | parcial |
| Inteligencia | `/Reports/Index` | Relatorios | padrao autenticado | nao executado |
| Inteligencia | `/Analytics/Executive` | BI Executivo | `Analytics.Executive` | nao executado |
| Inteligencia | `/Analytics/Forecast` | Forecast | `Analytics.Forecast` | nao executado |
| Inteligencia | `/Analytics/DataQuality` | Qualidade dos Dados | `DataQuality.View` | nao executado |
| Inteligencia | `/Analytics/AccountHealth` | Saude da Conta | `AccountHealth.View` | nao executado |
| Inteligencia | `/Alerts/Index` | Alertas | padrao autenticado | nao executado |
| Administracao | `/Services/Index` | Servicos | `services.read` | nao executado |
| Administracao | `/Templates/Index` | Templates | `templates.read` | nao executado |
| Administracao | `/Import/Index` | Importacao | padrao autenticado | nao executado |
| Administracao | `/Settings/Index` | Configuracoes | padrao autenticado | nao executado |
| Administracao | `/Admin/Quality/Index` | Qualidade funcional | `Quality.View` | nao executado |
| Refinamento | `/Admin/JourneyRefinement/Index` | Refinamento de Jornadas | `JourneyRefinement.View` | nao executado |
| Conta | `/Profile/Index` | Dados do emitente | padrao autenticado | nao executado |
| Conta | `/Notifications/Index` | Notificacoes | padrao autenticado | nao executado |
| Conta | `/Help/Index` | Base de conhecimento | `KnowledgeBase.View` | nao executado |
| Conta | `/Productivity/Index` | Produtividade | `Productivity.View` | nao executado |
| Conta | `/Support/Index` | Suporte | padrao autenticado | nao executado |

## Checklist de producao

| Item | Resultado | Observacao |
| --- | --- | --- |
| Configuracao de producao | nao executado | requer ambiente alvo |
| HTTPS e cookies seguros | nao executado | revisar deploy real |
| Segredos externos ao repositorio | parcial | `.env.example` existe, sem validacao de ambiente |
| Logs com correlacao | parcial | `TraceIdentifier` aparece em layouts/feedback; sem teste de vazamento |
| Health checks | parcial | rotas existem; sem validacao externa |
| Backup/restauracao | nao executado | procedimento nao demonstrado |
| Migration/rollback | nao executado | sem banco descartavel nesta rodada |
| Smoke pos-deploy | nao executado | sem deploy |
| Roteiro com usuario | parcial | este documento lista os cenarios, mas nao substitui execucao |

## Roteiro minimo de homologacao com usuario

1. Cadastro/login, selecao de conta e configuracao inicial.
2. Cliente: criar, consultar, editar e inativar quando permitido.
3. Servicos: criar item com unidade por nome, localizar e reutilizar no orcamento.
4. Orcamento: criar, autosalvar, revisar, finalizar, reler e gerar PDF.
5. IA: gerar sugestao, revisar quantidades, aplicar, repetir aplicacao e validar conflito.
6. Recebimento: registrar pagamento, emitir recibo, consultar e reemitir.
7. Plano/cobranca: simular aprovacao, duplicidade, evento fora de ordem e estorno.
8. Administracao: contas, usuarios, permissoes e auditoria.
9. PWA: login/logout/troca de conta sem cache privado reaparecer.
10. Acessibilidade: 360, 390, 768, 1024 e 1440 px, zoom 200%, teclado completo e foco visivel.

## Bloqueadores para producao

- Homologacao funcional das jornadas essenciais nao foi executada em navegador com dados reais.
- Testes PostgreSQL de concorrencia/tenant estao ignorados.
- Cobranca/webhook/estorno nao foram validados em sandbox.
- PDF final nao foi renderizado nem comparado com persistencia.
- Seguranca multi-conta, CSRF direto em handlers, XSS de IA e cache autenticado exigem execucao dedicada.
- Vulnerabilidades de dependencias e conflito EF precisam decisao tecnica antes da liberacao.
