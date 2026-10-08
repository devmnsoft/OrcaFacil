# Homologacao do OrçaFácil em 2026-10-07

Data: 2026-10-07  
Commit de referência: `b7b0d2fc24e3e518dabc5a509168f54b79ca81e3` (HEAD local, sem commit, sem push e sem deploy)  
Ambiente desta rodada: Windows, .NET 10, container `orcafacil-validation-pg` (Postgres 16) em `127.0.0.1:55433`, banco descartável `orcafacil_homolog`  
A porta `5432` deste computador pertence a outro produto. Nenhum comando desta rodada usou esse banco.  
Parecer: **NÃO APTO** para produção.

O parecer não muda porque a jornada autenticada no navegador, o sandbox de cobrança, o teste HTTP de CSRF/XSS, o aceite de uma pessoa e a troca de conta no PWA continuam sem execução. Build, testes de serviço e amostras de PDF não substituem esses cenários.

## Resultado da rodada com PostgreSQL e PDF

| Área | Cenário | Resultado | Evidência |
| --- | --- | --- | --- |
| Baseline | `git rev-parse HEAD` | executado com sucesso | `b7b0d2fc24e3e518dabc5a509168f54b79ca81e3`, alterações locais ainda não commitadas |
| Testes sem banco | filtro que exclui `JourneyPostgresTests` e `HomologationPostgresTests` | executado com sucesso | 569 aprovados, 0 falhas |
| PostgreSQL | jornada, concorrência de IA, cota, decisão pública e recibo | executado com sucesso | 9 fatos em `orcafacil_homolog`, duas conexões, 0 falhas |
| PDF | quatro amostras sem dados pessoais | executado com sucesso | `docs/homologacao/amostras/` |
| Menu, formulários, PWA em script, dependências | rodada anterior | executado com sucesso | seção abaixo; logout do PWA continua bloqueado |
| Navegador autenticado | cliente, orçamento, PDF baixado, recebimento e recibo | bloqueado | não há sessão autenticada nem ferramenta de navegador nesta sessão |
| IA Groq, Gemini e DeepSeek | sugestão real de cada provedor | bloqueado | provedores desligados; o teste de cota não é sucesso de provedor externo |
| Cobrança | checkout, webhook, repetição e estorno | bloqueado | sem sandbox Mercado Pago; mocks não foram usados |
| Segurança HTTP | troca de IDs, CSRF e XSS | não executado | o isolamento ensaiado foi de serviço: outra conta recebe `NotFound` e saldo zero |
| Design | 360, 390, 768, 1024, 1440 e zoom 200% | não executado | nenhuma captura nova de tela autenticada |
| Aceite do usuário | roteiro no final deste arquivo | não executado | depende de uma pessoa operando o produto |

Sem `ORCAFACIL_JOURNEY_CONNECTION`, os 6 fatos de `JourneyPostgresTests` e os 3 de `HomologationPostgresTests` são ignorados. Nesta rodada a variável apontou só para `orcafacil_homolog` e esses 9 fatos rodaram. O fato de PDF não usa o banco.

## O que o PostgreSQL comprovou

- Aplicação concorrente da revisão: as duas conexões produzem um único documento vinculado.
- Aplicação contra descarte: um único desfecho.
- Duas reservas no limite do plano: uma permitida e uma negada; repetir a correlação vencedora não chama outra reserva.
- Decisões públicas conflitantes: a primeira permanece; conflito serializável não estoura a operação.
- Recebimento repetido e recibos concorrentes: um recibo para o valor recebido.
- Outra conta não registra pagamento na ordem de serviço e o valor pago permanece zero.
- Conversão concorrente gera uma ordem de serviço.
- Idempotência de comando foi reexecutada no mesmo banco.

A numeração de documento continua sendo máximo mais um. Este ciclo não provou duas numerações simultâneas. O logo não entra no PDF: o modelo de amostra não tem imagem e o template não desenha logo. `partner_contacts`, `partner_profiles` e `suppliers` ainda têm a coluna física `whatsapp`, enquanto o modelo pede `whats_app`. Essas telas não entraram na jornada e continuam com a mesma classe de falha que `account_settings` tinha.

## Correções que destravaram o banco e o PDF

- `AccountSettings.WhatsApp` e `WhatsAppMessage` passam a usar `whatsapp` e `whatsapp_message`, que é o que `script_completop.sql` cria. A convenção snake case deixa de apagar um nome de coluna já configurado.
- O cartão de sugestão grava `updated_at` antes do insert. `clients.version` tem default e o hotfix `database/hotfix_client_rowversion_v70.sql` está no script consolidado e na migration `20261007225000_ClientRowVersionDefaultV70`.
- Colunas que faltavam em `document_items` (`unit`, `notes`, `sort_order` e snapshots) entram no mesmo hotfix aditivo.
- Decisão pública trata falha de serialização embrulhada pela estratégia de execução e relê a decisão já gravada.
- O PDF mostra subtotal e desconto quando o desconto do documento é maior que zero. As colunas de valor usam largura fixa. Cada célula de item evita quebrar a linha no meio. O valor `R$ 14.999.989,99` cabe na mesma linha do item.

## Amostras de PDF

Arquivos em `docs/homologacao/amostras/`, só com nomes genéricos:

| Arquivo | Páginas | Plano na amostra | Conferência |
| --- | --- | --- | --- |
| `orcamento-curto-sem-logo.pdf` | 1 | Professional | `ORC-000003`, total `R$ 260,00` (1,5 × 180 − 10). Sem marca de plano gratuito e sem logo |
| `orcamento-muitos-itens.pdf` | 5 | Free | `ORC-000048`, subtotal `R$ 60.639,25`, desconto `R$ 80,00`, total `R$ 60.559,25`. O item 24 começa inteiro na página 3. Rodapé “Gerado com OrçaFácil” |
| `orcamento-valor-alto-acentos.pdf` | 1 | Professional | `ORC-000099`, subtotal `R$ 34.999.989,97`, desconto `R$ 1.500,00`, total `R$ 34.998.489,97`. “ação, ç e ã” permanecem |
| `recibo-curto.pdf` | 1 | Free | `REC-000001`, subtotal `R$ 1.590,00`, desconto `R$ 40,00`, total `R$ 1.550,00` |

O extrator de texto omite ligaduras (`fi`, `ti`, `ft`). O rodapé no código é `comercial@mnsoft.com.br`. Não houve prévia de tela para comparar com o arquivo.

## Banco descartável, backup e migration

- Instalação limpa de `database/script_completop.sql` só funciona com os arquivos vizinhos do `\ir`. Copiar só o arquivo principal falha. Com a pasta inteira, o script termina e cria 296 tabelas em `orcafacil`.
- `dotnet ef database update` em banco vazio falha em `billing_customer_profiles`. Sobre o script com histórico vazio, falha em tabela duplicada (`email_outbox_messages`). Esse caminho não é o upgrade seguro. O banco parcial `orcafacil_clean_test` foi removido do container de validação.
- Backup demonstrado só no descartável: `pg_dump -Fc` de `orcafacil_homolog`, `pg_restore` em `orcafacil_homolog_restore`. Os dois ficaram com 296 tabelas, 19 clientes e 9 documentos. O banco restaurado e o arquivo de dump foram apagados em seguida. Os scripts `database/backup_postgres.ps1` e `restore_postgres.ps1` não rodaram: não há `pg_dump` no PATH do Windows e o padrão deles é a porta 5432.
- O papel `orcafacil_homolog` voltou a `NOSUPERUSER` depois dos testes. A senha ficou só num arquivo temporário da máquina e esse arquivo foi apagado ao fechar a rodada. Ela não está no repositório nem neste relatório.

## Preparação de produção

Sem deploy automático. Use o que já existe:

- Segredos: `docs/CONFIGURACAO-BANCO-LOCAL.md`, `ENVIRONMENTS.md` e user secrets. Provedores e Mercado Pago permanecem desligados até configuração válida.
- HTTPS, cookies e IIS: `DEPLOY.md`, `DEPLOY-IIS.md` e `docs/DEPLOY-IIS-ASPNET.md`. Não houve ensaio no destino.
- Correlação: `TraceIdentifier` já alimenta layout e feedback. Não houve ensaio de log com conteúdo sensível.
- Backup de produção: repetir o par `pg_dump` / `pg_restore` no destino, com o script do repositório apontando para o host certo. O ensaio desta rodada não restaura produção.
- Migration: instalação nova pelo `script_completop.sql` e a pasta `database/`. Upgrade de banco que já tem o script deve aplicar só hotfixes aditivos, não `dotnet ef database update` sobre histórico vazio.
- Smoke depois do deploy: login, `/health`, um orçamento, um PDF e um recibo na conta de ensaio.
- Aceite: o roteiro no final deste arquivo.

Voz, embeddings e automações externas ficam no backlog.

## Rodada da estabilização visual, antes do PostgreSQL

Commit analisado: `b7b0d2fc24e3e518dabc5a509168f54b79ca81e3`  
Ambiente daquela passagem: Windows, .NET 10, Edge headless, PostgreSQL em `127.0.0.1:5432` sem connection string de homologação.

## Resultado da rodada visual

| Área | Cenário | Resultado | Evidência |
| --- | --- | --- | --- |
| Baseline | `git rev-parse HEAD` | executado com sucesso | `b7b0d2fc24e3e518dabc5a509168f54b79ca81e3` |
| Build e testes | `dotnet test OrcaFacil.sln` | executado com sucesso | 0 erros, 47 avisos de compilação/analisador, 568 aprovados, 6 ignorados |
| Menu sem `inert` | abrir/fechar 3 vezes, Escape e foco | executado com sucesso | script Node sobre `app.js`: `tabindex` ausente, `0`, `2`, `-1` e botão desabilitado restaurados |
| Formulários | disabled, readonly, erro, link e botão secundário | executado com sucesso | Edge headless, estilo computado; contraste mínimo medido 4,56:1 no link do cartão claro |
| PWA | instalação, versão e caches alheios | executado com sucesso na revisão do script | `sw.js` não chama `clients.claim`; só apaga caches `orcafacil-public-`; navegação autenticada não é armazenada |
| PWA | logout e troca de conta no navegador | bloqueado | sem sessão autenticada |
| Dependências | `dotnet list package --vulnerable --include-transitive` | executado com sucesso | nenhum pacote vulnerável nas fontes NuGet atuais |
| EF | alinhamento de `Relational` | executado com sucesso | Web, Domain, Shared e Persistence resolvem `10.0.10`; Npgsql provider permanece `10.0.3` |
| IA | aplicação, descarte, quantidade e cota no código | executado com sucesso na revisão | compare-and-set no descarte; cota usa o limite do plano efetivo; sem prova PostgreSQL de concorrência |
| Jornada comercial | cliente até recibo no navegador | bloqueado | sem conta de teste autenticada |
| IA Groq, Gemini e DeepSeek | sugestão real | bloqueado | provedores desligados até configuração válida |
| PDF | renderização de orçamento e recibo | não executado | sem geração nesta rodada |
| Cobrança | checkout, webhook e estorno | bloqueado | sem sandbox Mercado Pago |
| Segurança HTTP | troca de IDs, CSRF e XSS | não executado | sem duas contas e sem servidor de homologação |
| Backup | restauração demonstrada | não executado | procedimento em `database/restore_postgres.ps1` e `docs/ROLLBACK-V1.md`, sem ensaio |
| Aceite do usuário | roteiro abaixo | não executado | depende de pessoa operando o produto |

Os 6 testes ignorados são `JourneyPostgresTests`. O motivo é a ausência de `ORCAFACIL_JOURNEY_CONNECTION`. A porta 5432 está aberta, mas nenhuma senha foi lida nem gravada para criar um banco descartável.

## Correções desta rodada

- Menu móvel: a lista de foco guarda o `tabindex` original na primeira vez e o devolve na reabertura. Com `inert`, o painel fechado e o fundo aberto ficam inativos. Sem `inert`, o mesmo efeito usa a lista estável. O painel não recebe `role="dialog"` nem `aria-modal`. Em largura de desktop o overlay fecha.
- O mesmo comportamento vale para o menu real de `#client-sidebar` em `experience.js`.
- Formulários: estados disabled, readonly, erro e autofill vencem a superfície escura. Link e botão secundário dentro de cartão claro mantêm texto escuro ou a cor primária. Placeholder do token passou a `#5f6f82`. Erro sobre fundo escuro usa `#ffd7d1`.
- PWA: um asset ausente não impede o registro. A página antiga continua com o service worker antigo até o recarregamento. O aviso de atualização não promete operação offline. O banner offline continua a bloquear POST sem conexão.
- Pacotes: `Microsoft.AspNetCore.OpenApi` 10.0.12 com `Microsoft.OpenApi` 2.12.2 (GHSA-v5pm-xwqc-g5wc, corrigido a partir de 2.7.5). `MailKit` e `MimeKit` 4.18.1 (CVE-2026-30227, corrigido em MimeKit 4.15.1). EF Relational fixado em 10.0.10, compatível com Npgsql 10.0.3 (`>= 10.0.4 < 11`).
- IA: descarte e aplicação usam compare-and-set em `PendingReview`. Seleção acima de 30 itens é recusada. Quantidade com vírgula ou ponto é lida; texto inválido não reaproveita a quantidade sugerida em silêncio. A cota mensal efetiva é o menor valor entre a configuração operacional e `ai.monthly_limit` do plano. Cancelamento grava consumo com status `Cancelled`. Não houve teste de duas conexões PostgreSQL.

A correção anterior do `Scenario` em `BudgetSuggestionApplyTests` e a extração de `pipeline.css` foram preservadas.

## Avisos que permanecem

São 47 avisos de compilação e analisador, sem vulnerabilidade NuGet associada. Os grupos são nulabilidade (`CS8602`, `CS8604`, `CS8601`), API obsoleta de check constraint do EF (`CS0618`), membros ocultos (`CS0108`) e três avisos `xUnit2031`. Não foram suprimidos. Impacto: não impedem o build nem indicam falha de pacote; a dívida de nulabilidade no onboarding continua sem correção neste ciclo.

## Preparação de produção ainda não ensaiada

Use os runbooks existentes, sem deploy automático:

- Configuração e segredos: `docs/CONFIGURACAO-BANCO-LOCAL.md`, `ENVIRONMENTS.md` e user secrets. Nenhum segredo entra no repositório.
- Banco descartável: `scripts/windows/prepare-homologation.ps1` exige `ORCAFACIL_HOMOLOG_DATABASE_URL`, host local e nome terminado em `_homolog` ou `_test`.
- HTTPS, cookies e IIS: `DEPLOY.md`, `DEPLOY-IIS.md` e `docs/DEPLOY-IIS-ASPNET.md`.
- Correlação: o `TraceIdentifier` já alimenta layouts e feedback. Falta ensaio de log sem conteúdo sensível.
- Backup e restauração: `database/backup_postgres.ps1`, `database/restore_postgres.ps1`, `BACKUP.md` e `docs/ROLLBACK-V1.md`.
- Migration: `scripts/windows/update-database.ps1` aplica só SQL dentro de `database/`. Não houve migration nova neste ciclo.
- Smoke pós-deploy: login, `/health`, um orçamento, um PDF e um recibo na conta de ensaio.
- Aceite: o roteiro mínimo abaixo.

Provedores de IA e Mercado Pago permanecem desligados até a configuração válida do ambiente. Voz, embeddings e automações externas ficam no backlog.

## Rodada anterior, preservada

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
| Backup/restauracao | executado no descartavel | `pg_dump`/`pg_restore` em `orcafacil_homolog`; 296 tabelas, 19 clientes e 9 documentos conferidos; banco restaurado apagado. Scripts do Windows nao rodaram |
| Migration/rollback | parcial | `script_completop.sql` instala limpo com os `\ir`. `dotnet ef database update` nao e upgrade seguro sobre historico vazio |
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

- NÃO APTO: a jornada cliente, orçamento, PDF baixado na tela, recebimento e recibo não foi executada em navegador autenticado.
- NÃO APTO: Groq, Gemini e DeepSeek não foram chamados. O teste de cota usa o serviço de reserva, não um provedor externo.
- NÃO APTO: checkout, webhook, repetição e estorno não foram executados em sandbox. O retorno do navegador não foi ensaiado.
- NÃO APTO: troca de IDs por HTTP, CSRF e XSS não foram executados. O isolamento comprovado é o de serviço, com `NotFound` para a outra conta.
- NÃO APTO: larguras, zoom, teclado e logout/troca de conta do PWA não tiveram captura nesta rodada.
- NÃO APTO: não houve aceite de uma pessoa no roteiro abaixo.
- Deixaram de ser bloqueadores desta auditoria: vulnerabilidades NuGet já tratadas, conflito EF 10.0.4/10.0.10, os 9 fatos PostgreSQL da jornada e a geração das quatro amostras de PDF. O logo grande continua fora do template. A numeração concorrente de documento não foi provada.
