import fs from 'node:fs';
const required = [
  'src/OrcaFacil.Domain/Entities/AssetManagement.cs',
  'src/OrcaFacil.Persistence/Services/AssetOperationsService.cs',
  'database/patch_sprint43_assets_v44.sql'
];
const failures = [];
for (const file of required) if (!fs.existsSync(file)) failures.push(`arquivo obrigatório ausente: ${file}`);
const sources = required.filter(fs.existsSync).map(file => [file, fs.readFileSync(file, 'utf8')]);
const service = sources.find(([file]) => file.includes('AssetOperationsService'))?.[1] ?? '';
for (const rule of ['x.AccountId == accountId', 'assetIds.Count == 0', 'MaintenanceGeneratedWorkOrders.AnyAsync', 'RandomNumberGenerator.GetBytes', 'string.IsNullOrWhiteSpace(report.Conclusion)', 'item.IsCritical'])
  if (!service.includes(rule)) failures.push(`regra crítica ausente: ${rule}`);
for (const [file, text] of sources) {
  for (const pattern of [/Math\.random\s*\(/i, /NotImplementedException/, /javascript\s*:/i, /href\s*=\s*["']#["']/i])
    if (pattern.test(text)) failures.push(`${file}: padrão proibido ${pattern}`);
}
const sql = sources.find(([file]) => file.endsWith('.sql'))?.[1] ?? '';
for (const table of ['customer_assets','maintenance_plans','asset_inspections','non_conformities','corrective_action_plans','technical_reports','asset_qr_codes','asset_qr_access_logs'])
  if (!sql.includes(`CREATE TABLE IF NOT EXISTS orcafacil.${table}`)) failures.push(`tabela idempotente ausente: ${table}`);
if (!sql.includes('ux_preventive_generation_period')) failures.push('índice de idempotência preventiva ausente');
if (failures.length) { console.error(failures.map(x => `✗ ${x}`).join('\n')); process.exit(1); }
console.log('✓ Sprint 43: ativos, manutenção, inspeções, qualidade e segurança validados.');
