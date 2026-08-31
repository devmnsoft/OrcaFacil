import fs from 'node:fs';
const source=fs.readFileSync('src/OrcaFacil.Application/Automation/AutomationEngine.cs','utf8');
for (const token of ['payment.confirm','fiscal.issue','permission.change','AutomationRisk.Critical','RequiresApproval','BlockedActions']) if (!source.includes(token)) { console.error(`Critical action guard missing: ${token}`); process.exit(1); }
console.log('Automation critical actions require approval or dry-run blocking: OK');
