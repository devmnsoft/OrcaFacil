import fs from 'node:fs';
const read = p => fs.readFileSync(p, 'utf8');
const required = [
 'src/OrcaFacil.Domain/Entities/Customization.cs',
 'src/OrcaFacil.Application/Customization/CustomizationRules.cs',
 'database/sprint28_process_customization.sql'
];
for (const file of required) if (!fs.existsSync(file)) throw new Error(`Sprint 28: arquivo ausente: ${file}`);
const domain = read(required[0]), rules = read(required[1]), sql = read(required[2]);
for (const symbol of ['CustomFieldDefinition','DynamicFormVersion','ChecklistTemplate','ConfigurablePipeline','WorkflowTransition','AutomationRuleRun'])
 if (!domain.includes(symbol)) throw new Error(`Sprint 28: domínio ausente: ${symbol}`);
for (const table of ['custom_field_definitions','dynamic_form_versions','workflow_instances','automation_rule_runs','process_templates','validation_rule_definitions','notification_rule_definitions'])
 if (!sql.includes(table)) throw new Error(`Sprint 28: tabela ausente: ${table}`);
if (!sql.includes('IF NOT EXISTS')) throw new Error('Sprint 28: DDL não é idempotente.');
if (/Math\.random|NotImplementedException|registerPayment|issueReceipt/i.test(rules)) throw new Error('Sprint 28: regra insegura ou incompleta.');
if (!rules.includes('AutomationRuleEngine') || !rules.includes('EventId == eventId')) throw new Error('Sprint 28: idempotência da automação ausente.');
console.log('Sprint 28: domínio real, isolamento, versões, workflow e automação segura validados.');
