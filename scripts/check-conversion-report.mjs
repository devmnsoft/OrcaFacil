import { readFileSync } from 'node:fs';

const service = readFileSync('src/OrcaFacil.Web/Services/IntelligenceReportService.cs', 'utf8');
const rules = readFileSync('src/OrcaFacil.Web/Services/ConversionReportRules.cs', 'utf8');
const page = readFileSync('src/OrcaFacil.Web/Pages/Reports/Conversion.cshtml.cs', 'utf8');
const view = readFileSync('src/OrcaFacil.Web/Pages/Reports/Conversion.cshtml', 'utf8');
const hubCs = readFileSync('src/OrcaFacil.Web/Pages/Reports/Index.cshtml.cs', 'utf8');
const hubView = readFileSync('src/OrcaFacil.Web/Pages/Reports/Index.cshtml', 'utf8');
const tests = readFileSync('tests/OrcaFacil.UnitTests/ConversionReportRulesTests.cs', 'utf8');
const failures = [];

// ---------- 1. O serviço expõe o relatório de conversão ----------
if (!service.includes('Task<IntelligenceReport> ConversionAsync('))
  failures.push('IIntelligenceReportService sem o relatório de conversão (ConversionAsync).');
if (!service.includes('x.ClientDecision == ClientDecision.Approved'))
  failures.push('relatório de conversão não parte das propostas aprovadas.');

// ---------- 2. Recebimentos isolados por conta e apenas ativos ----------
if (!service.includes('p.AccountId == accountId && !p.IsDeleted && p.Status == FinancialRecordStatus.Active'))
  failures.push('recebimentos da conversão sem isolamento por conta/status ativo.');

// ---------- 3. Regra pura: conversão é OS OU recibo ----------
if (!rules.includes('row.ConvertedToWorkOrder || row.ConvertedToReceipt'))
  failures.push('conversão deve considerar ordem de serviço OU recibo (união).');
if (!rules.includes('"ConvertedToWorkOrder"'))
  failures.push('status de conversão em OS não reconhecido nas regras puras.');

// ---------- 4. Guarda de divisão (dados insuficientes = -1) ----------
if (!rules.includes('rows.Count == 0 ? -1m : rows.Count(IsConverted) * 100m / rows.Count'))
  failures.push('taxa de conversão sem guarda de divisão para período sem aprovações.');

// ---------- 5. Linhas por cliente ----------
if (!rules.includes('GroupBy(x => x.ClientName)'))
  failures.push('linhas por cliente ausentes do relatório de conversão.');

// ---------- 6. Página reusa a base de relatórios + gate do plano ----------
if (!page.includes('FilePrefix => "conversao-comercial"'))
  failures.push('export CSV do relatório de conversão sem prefixo próprio.');
if (!page.includes('PlanFeatureCodes.CommercialMetricsEnabled'))
  failures.push('relatório de conversão sem o gate do recurso de métricas comerciais.');
if (!page.includes('reports.ConversionAsync(f, ct)'))
  failures.push('página de conversão não carrega o relatório do serviço.');
if (!view.includes('<partial name="_Report" model="Model" />'))
  failures.push('visualização de conversão sem o partial _Report compartilhado.');

// ---------- 7. A central de inteligência publica o relatório ----------
if (!hubCs.includes('Conversion = await reports.ConversionAsync(filter, ct)'))
  failures.push('central de inteligência não carrega a conversão comercial.');
if (!hubView.includes('asp-page="/Reports/Conversion"'))
  failures.push('central de inteligência sem o tile do relatório de conversão.');

// ---------- 8. Os testes fixam as regras puras ----------
for (const [name, token] of [
  ['taxa com guarda de divisão', 'ConversionRate_returns_minus_one_without_approved_budgets'],
  ['tempo até converter com fallbacks', 'AverageConversionDays_uses_decision_to_stage_span_with_fallbacks'],
  ['linhas por cliente ordenadas', 'BuildClientRows_groups_by_client_orders_by_converted_value'],
])
  if (!tests.includes(token)) failures.push(`teste das regras de conversão ausente: ${name}`);

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Relatório de conversão validado: aprovado -> OS/recibo -> recebido, com taxa, tempo médio, linhas por cliente, gate de plano e CSV herdados da base.');
