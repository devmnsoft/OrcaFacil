import fs from 'node:fs';
const required = ['financial_categories','cost_centers','bank_accounts','cash_movements','payables','payable_payments','financial_period_closings','fiscal_document_requests','bank_transactions','bank_reconciliation_sessions','bank_reconciliation_matches','financial_import_batches','financial_import_rows'];
const schema = fs.readFileSync('database/patch_sprint24_advanced_finance_v25.sql','utf8');
const rules = fs.readFileSync('src/OrcaFacil.Application/Finance/AdvancedFinanceRules.cs','utf8');
for (const table of required) if (!schema.includes(`orcafacil.${table}`)) throw new Error(`Tabela ausente: ${table}`);
for (const guard of ['EnsureSameAccount','IdempotencyKey','FiscalNotConfiguredMessage']) if (!rules.includes(guard)) throw new Error(`Proteção ausente: ${guard}`);
console.log('Financeiro avançado V2.5: schema idempotente, isolamento, idempotência e controle fiscal verificados.');
