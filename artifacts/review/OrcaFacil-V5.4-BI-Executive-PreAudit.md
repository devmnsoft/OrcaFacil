# Pré-auditoria — BI Executivo V5.4

Data: 2026-09-01

## Escopo e bloqueios

- Auditoria limitada à solução ASP.NET atual e aos seus scripts operacionais.
- Não foram adicionadas dependências de aplicações legadas, CDN ou frameworks de UI.
- O ambiente de execução não disponibiliza o SDK `dotnet`; builds e testes precisam ser repetidos em agente com .NET instalado.
- `npm ci` foi bloqueado com HTTP 403 para `javascript-obfuscator`; os módulos versionados foram preservados.

## Decisões de segurança

- Métricas são sempre calculadas com `AccountId`, período, fonte e permissão explícita.
- Métricas financeiras sensíveis exigem `BI.SensitiveFinancialMetricsView` no backend.
- Forecast sem três pontos históricos retorna dados insuficientes e sempre preserva premissas.
- Dashboards, metas, OKRs, alertas, snapshots e insights carregam escopo de conta.
- Nenhum valor aleatório ou indicador demonstrativo foi introduzido.

## Pendências operacionais

- Aplicar `database/sprint53_bi_executive_v54.sql` em ambiente de homologação com backup e validar o plano dos índices.
- Conectar `IBiMetricDataSource` às consultas filtradas dos módulos habilitados, antes de expor cada KPI no cockpit.
- Executar validação manual autenticada, responsiva e de console em ambiente com banco e navegador disponíveis.
