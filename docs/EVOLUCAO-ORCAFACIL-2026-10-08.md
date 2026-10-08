# Evolucao OrçaFacil - 2026-10-08

## Baseline

- Branch: `main`.
- Commit inicial observado: `b7b0d2fc24e3e518dabc5a509168f54b79ca81e3`.
- `AGENTS.md`: nao encontrado.
- Projetos publicados inspecionados neste corte: `src/OrcaFacil.Web` e `src/OrcaFacil.Api`.
- Arvore de trabalho inicial ja continha alteracoes locais em codigo, docs, database, `.vs`, `bin` e `obj`; este corte preservou essas alteracoes.
- Migrador/producao: nenhuma migration foi executada e nenhum banco de producao foi tocado.

## Diagnostico Confirmado

- `src/OrcaFacil.Api/Controllers/PublicQuotesController.cs` ainda retornava `202 Accepted` na recusa sem gravar decisao.
- O mesmo controller retornava `Array.Empty<byte>()` como PDF publico.
- `src/OrcaFacil.Api/Controllers/DocumentsController.cs` ainda expunha recibo pelo caminho legado `DocumentService.CreateReceiptAsync`.
- O endpoint antigo de link publico da API ainda chamava `DocumentService.GeneratePublicLinkAsync`, que grava token bruto em `PublicQuote` e auditoria.
- A pagina Razor publica ja usava `IPublicDocumentAccessService` e `ICommercialJourneyService`, portanto a regra canonica existente estava disponivel para unificar a API.

## Mudancas Implementadas

- API publica de aprovacao/recusa passou a chamar `ICommercialJourneyService.DecideAsync`.
- Recusa publica agora exige persistencia efetiva e recebe nome, contato, motivo, mensagem opcional e chave de idempotencia.
- PDF publico agora abre o token por `IPublicDocumentAccessService.OpenAsync`, respeitando link invalido, expirado, revogado e revisao antiga.
- PDF publico e gerado a partir do snapshot da revisao aberta, nao do documento mutavel atual.
- `POST /api/documents/{id}/public-link` passou a usar `ICommercialJourneyService.CreatePublicAccessAsync`.
- `POST /api/documents/receipt` agora bloqueia emissao pelo caminho legado sem pagamento correspondente.
- `src/OrcaFacil.Api/Program.cs` recebeu os registros de DI necessarios para a jornada comercial canonica.
- Adicionado `src/OrcaFacil.Api/Services/ApiCurrentAccountService.cs` para resolver conta ativa na API autenticada.

## Validacao Executada

- `dotnet restore OrcaFacil.sln`: sucesso.
- `dotnet build OrcaFacil.sln`: sucesso, 0 erros, 0 avisos.
- `dotnet test OrcaFacil.sln`: sucesso; 569 aprovados, 9 ignorados.
- `dotnet list OrcaFacil.sln package --vulnerable --include-transitive`: nenhum pacote vulneravel reportado nas fontes atuais.

## Nao Executado / Bloqueado

- Testes `JourneyPostgresTests` e `HomologationPostgresTests`: ignorados pela suite, pois nao havia `ORCAFACIL_JOURNEY_CONNECTION` configurada para banco dedicado.
- Navegador autenticado, screenshots responsivos, sandbox de cobranca, renderizacao visual de PDF e teste real entre duas contas: nao executados neste corte.
- Migrations e script consolidado: nao alterados neste corte.

## Parecer

Bloqueado para liberacao de producao completa.

O corte remove os sucessos ficticios mais criticos da API publica e bloqueia um caminho legado de recibo sem pagamento, com build e testes unitarios aprovados. A homologacao SaaS completa ainda depende de banco PostgreSQL descartavel, testes autenticados entre contas, validacao visual/PDF, sandbox de cobranca e decisao sobre a politica de trial de 15 dias.

## Corte de estabilizacao - numeracao concorrente

Data: 2026-10-08  
Commit de referencia do pedido: `4e37e8028ef0bc2cd80c3a717d58de560b799eeb`  
Ambiente: Windows, .NET 10, sem `ORCAFACIL_JOURNEY_CONNECTION` configurada nesta execucao.

### Mudancas

- Adicionada a migration incremental `20261008090000_AddDocumentSequencesV71` para criar `orcafacil.document_sequences`, sem renumerar documentos existentes.
- O script consolidado `database/script_completop.sql` agora cria e semeia `document_sequences` a partir dos documentos existentes com `account_id`.
- `BudgetWizardService` passou a solicitar numero com `AccountId`, permitindo numeracao por conta.
- Web e API registram `AtomicDocumentNumberService`, que usa `INSERT ... ON CONFLICT`, `SELECT ... FOR UPDATE` e `UPDATE` no PostgreSQL para alocar o proximo numero por conta e tipo.
- O servico antigo de numeracao por usuario permanece apenas como fallback quando nao ha conta ativa, mantendo compatibilidade com caminhos legados que ainda nao foram unificados.
- Adicionado indice unico parcial `ux_documents_account_type_number` para impedir duplicidade por conta, tipo e numero em documentos ativos.

### Validacao

- `dotnet restore OrcaFacil.sln`: sucesso.
- `dotnet build OrcaFacil.sln`: sucesso, 0 erros, 47 avisos pre-existentes.
- `dotnet test OrcaFacil.sln`: sucesso; 569 aprovados, 9 ignorados.
- `dotnet list OrcaFacil.sln package --vulnerable --include-transitive`: nenhum pacote vulneravel reportado nas fontes atuais.

### Pendencias

- A concorrencia real de duas conexoes PostgreSQL para numeracao ainda nao foi executada nesta rodada porque nao havia banco dedicado configurado.
- A API autenticada ainda possui caminhos de criacao/edicao/PDF que precisam ser unificados com conta ativa, permissao e plano efetivo.
- O procedimento oficial de instalacao/upgrade ainda precisa ser validado em bancos descartaveis criados por SQL e por migrator.
