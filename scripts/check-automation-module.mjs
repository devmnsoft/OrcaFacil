import fs from 'node:fs';
const read = p => fs.readFileSync(p,'utf8');
const engine = read('src/OrcaFacil.Application/Automation/AutomationEngine.cs');
const required = ['AutomationTriggerCatalogService','AutomationConditionEvaluator','AutomationRuleBuilderService','AutomationDryRunService','AutomationEventQueueService','AutomationApprovalService','AutomationSafetyPolicyService','IdempotencyKey','RetryDelay'];
const missing = required.filter(x=>!engine.includes(x));
if (missing.length) { console.error(`Automation module missing: ${missing.join(', ')}`); process.exit(1); }
console.log('Automation module contracts and safety controls: OK');
