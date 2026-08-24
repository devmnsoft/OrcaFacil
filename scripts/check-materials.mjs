import fs from 'node:fs';
const required = [
  'src/OrcaFacil.Domain/Entities/SupplyChain.cs',
  'src/OrcaFacil.Application/Inventory/SupplyChainRules.cs',
  'src/OrcaFacil.Persistence/Configurations/SupplyChainConfiguration.cs',
  'database/patch_sprint23_supply_chain_v24.sql'
];
const missing = required.filter(file => !fs.existsSync(file));
if (missing.length) throw new Error(`Sprint 23 incompleta: ${missing.join(', ')}`);
const domain = fs.readFileSync(required[0], 'utf8');
const rules = fs.readFileSync(required[1], 'utf8');
const sql = fs.readFileSync(required[3], 'utf8');
for (const token of ['Supplier', 'Material', 'InventoryItem', 'InventoryStockMovement', 'InventoryReservation', 'PurchaseRequest', 'PurchaseOrder', 'CostComposition', 'DocumentCostSnapshot', 'MarginPolicy'])
  if (!domain.includes(`class ${token}`)) throw new Error(`Entidade ausente: ${token}`);
for (const token of ['EnsureSameAccount', 'Saldo insuficiente', 'Ajuste de estoque exige motivo', 'Quantidade recebida excede'])
  if (!rules.includes(token)) throw new Error(`Regra crítica ausente: ${token}`);
for (const table of ['suppliers','materials','inventory_items','inventory_stock_movements','inventory_reservations','purchase_requests','purchase_orders','cost_compositions','document_cost_snapshots','margin_policies'])
  if (!sql.includes(`CREATE TABLE IF NOT EXISTS orcafacil.${table}`)) throw new Error(`Tabela idempotente ausente: ${table}`);
const publicPages = fs.readdirSync('src/OrcaFacil.Web/Pages/PublicQuotes').filter(x => x.endsWith('.cshtml')).map(x => fs.readFileSync(`src/OrcaFacil.Web/Pages/PublicQuotes/${x}`, 'utf8')).join('\n');
if (/EstimatedCost|MarginPercent|SupplierId|QuantityOnHand/.test(publicPages)) throw new Error('Proposta pública expõe dados internos de custo/estoque.');
console.log('Sprint 23: núcleo multiempresa de fornecedores, materiais, estoque, compras, custos e margem validado.');
