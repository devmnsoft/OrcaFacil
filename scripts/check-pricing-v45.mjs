import fs from 'node:fs';
const required = [
  'src/OrcaFacil.Domain/Entities/AdvancedPricing.cs',
  'src/OrcaFacil.Application/Pricing/AdvancedPricingRules.cs',
  'src/OrcaFacil.Persistence/Configurations/AdvancedPricingConfiguration.cs',
  'database/patch_sprint44_pricing_v45.sql'
];
for (const file of required) if (!fs.existsSync(file)) throw new Error(`Artefato V4.5 ausente: ${file}`);
const rules = fs.readFileSync(required[1], 'utf8');
const sql = fs.readFileSync(required[3], 'utf8');
for (const token of ['SelectTable', 'RequiresApproval', 'Snapshot', 'Decide']) if (!rules.includes(token)) throw new Error(`Regra ausente: ${token}`);
for (const table of ['service_price_tables','service_price_table_items','pricing_margin_policies','pricing_discount_policies','pricing_quote_snapshots','pricing_approval_events']) if (!sql.includes(`CREATE TABLE IF NOT EXISTS orcafacil.${table}`)) throw new Error(`Tabela idempotente ausente: ${table}`);
const pricingFiles = [...required, 'src/OrcaFacil.Application/Pricing/PricingEngineService.cs'].map(x => fs.readFileSync(x, 'utf8')).join('\n');
if (/Math\.random|NotImplementedException|TODO/i.test(pricingFiles)) throw new Error('Precificação contém implementação simulada ou incompleta.');
const publicDir = 'src/OrcaFacil.Web/Pages/PublicQuotes';
if (fs.existsSync(publicDir)) {
  const publicViews = fs.readdirSync(publicDir).filter(x => x.endsWith('.cshtml')).map(x => fs.readFileSync(`${publicDir}/${x}`, 'utf8')).join('\n');
  if (/TotalCost|MarginPercentage|EstimatedCost/.test(publicViews)) throw new Error('Proposta pública expõe custo ou margem.');
}
console.log('Sprint 44: tabelas, políticas, snapshots e aprovações comerciais validados.');
