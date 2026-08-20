import fs from 'node:fs';
const read = p => fs.readFileSync(p, 'utf8');
const checks = [
 ['Meu Plano/assinatura', 'src/OrcaFacil.Web/Pages/Subscription/Index.cshtml'],
 ['preços públicos reais', 'src/OrcaFacil.Web/Pages/Precos.cshtml'],
 ['serviço de pagamento manual', 'src/OrcaFacil.Application/Billing/BillingPaymentService.cs'],
 ['entitlements persistidos', 'src/OrcaFacil.Domain/Entities/SaasBilling.cs'],
 ['schema idempotente', 'database/patch_sprint18_billing_v19.sql']
];
for (const [label,path] of checks) { if (!fs.existsSync(path)) throw new Error(`${label}: ausente (${path})`); }
const pricing=read(checks[1][1]);
if (/asp-page=\"[^\"]*checkout|marcar\s+(como\s+)?pag[oa]/i.test(pricing)) throw new Error('Preços contém checkout/aprovação não configurada.');
if (!pricing.includes('contratação online ainda não está habilitada')) throw new Error('Preços deve comunicar o fluxo manual.');
const payment=read(checks[2][1]);
for (const rule of ['amount <= 0','invoice.AccountId != accountId','invoice.ApplyPayment','payment.Reverse']) if (!payment.includes(rule)) throw new Error(`Regra de pagamento ausente: ${rule}`);
const sql=read(checks[4][1]);
if (!sql.includes('CREATE TABLE IF NOT EXISTS') || !sql.includes('CREATE INDEX IF NOT EXISTS')) throw new Error('Patch de billing não é idempotente.');
if (!read('src/OrcaFacil.Web/Program.cs').includes('"/MeuPlano"')) throw new Error('A rota /MeuPlano não foi publicada.');
console.log('✓ monetização V1.9: contratos, cobrança manual, isolamento por conta e ausência de checkout simulado validados');
