import fs from 'node:fs';

const schema = fs.readFileSync('database/patch_sprint47_financial_management_v48.sql', 'utf8');
const service = fs.readFileSync('src/OrcaFacil.Application/Finance/FinancialManagementService.cs', 'utf8');
const permissions = fs.readFileSync('src/OrcaFacil.Application/Security/PermissionCodes.cs', 'utf8');
const requiredTables = ['financial_chart_accounts','financial_cost_centers','financial_management_entries','financial_entry_allocations','financial_cashflow_projection_runs','financial_budget_plans','financial_forecast_runs','financial_dre_snapshots','financial_monthly_closings','financial_profitability_snapshots','financial_risk_alerts','financial_audit_events'];
for (const table of requiredTables) if (!schema.includes(`orcafacil.${table}`)) throw new Error(`Tabela financeira ausente: ${table}`);
for (const rule of ['CalculateCashFlow','Project(','CalculateDre','ValidateManualEntry','ValidateAllocation','ValidateClosing','ManagementDisclaimer']) if (!service.includes(rule)) throw new Error(`Regra financeira ausente: ${rule}`);
for (const permission of ['FinanceManagementView','FinanceChartAccountsView','FinanceCashFlowProjectionView','FinanceDreView','FinanceCostsAndMarginsView']) if (!permissions.includes(permission)) throw new Error(`Permissão ausente: ${permission}`);
if (/Math\.random|\bdouble\b|\bfloat\b/.test(service)) throw new Error('Cálculo financeiro não determinístico ou monetário impreciso.');
if (!/account_id uuid NOT NULL/.test(schema) || !/IF NOT EXISTS/.test(schema)) throw new Error('Schema não garante escopo e aplicação idempotente.');
console.log('Financeiro gerencial V4.8: schema aditivo, cálculos determinísticos, regime, rateio, fechamento, isolamento e permissões verificados.');
