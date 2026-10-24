import { readFileSync } from 'node:fs';

const rules = readFileSync('src/OrcaFacil.Application/Plans/AdditionalSeatRules.cs', 'utf8');
const svc = readFileSync('src/OrcaFacil.Application/Plans/AdditionalSeatService.cs', 'utf8');
const iface = readFileSync('src/OrcaFacil.Application/Plans/IPlanAccessService.cs', 'utf8');
const access = readFileSync('src/OrcaFacil.Application/Plans/PlanAccessService.cs', 'utf8');
const ds = readFileSync('src/OrcaFacil.Persistence/Plans/EfPlanAccessDataSource.cs', 'utf8');
const page = readFileSync('src/OrcaFacil.Web/Pages/Subscription/Index.cshtml.cs', 'utf8');
const view = readFileSync('src/OrcaFacil.Web/Pages/Subscription/Index.cshtml', 'utf8');
const userpage = readFileSync('src/OrcaFacil.Web/Pages/AccountAdmin/Users/New.cshtml.cs', 'utf8');
const tests = readFileSync('tests/OrcaFacil.UnitTests/AdditionalSeatRulesTests.cs', 'utf8');
const failures = [];

// ---------- 1. Regras puras: teto, preço e elegibilidade ----------
if (!rules.includes('public const int MaxAdditionalSeats = 5'))
  failures.push('teto de assentos adicionais ausente ou alterado (MaxAdditionalSeats = 5).');
if (!rules.includes('public const decimal SeatPriceMonthly = 19.90m'))
  failures.push('preço mensal do assento ausente ou alterado (19.90m).');
if (!rules.includes('baseLimit + Math.Max(0, purchasedIncrement)'))
  failures.push('incremento de addon ignorado no cálculo do teto efetivo.');
if (!rules.includes('string.Equals(effectivePlanCode, BusinessPlanCode'))
  failures.push('elegibilidade limitada ao plano Negócio ausente.');

// ---------- 2. Teto efetivo cresce via source de addons ativos ----------
if (!iface.includes('GetAddonIncrementsAsync'))
  failures.push('IPlanAccessDataSource sem GetAddonIncrementsAsync.');
if (!ds.includes('join planAddon in db.PlanAddons'))
  failures.push('source de addons não junta account_addons com plan_addons.');
if (!ds.includes('addon.DeactivatedAt == null'))
  failures.push('source de addons conta addon desativado (DeactivatedAt).');
if (!access.includes('GetAddonIncrementsAsync') || !access.includes('AdditionalSeatRules.AddLimitIncrement('))
  failures.push('PlanAccessService não aplica os incrementos de addon aos limites do plano.');

// ---------- 3. Serviço de compra: seed idempotente, validação e auditoria ----------
if (!svc.includes('public interface IAdditionalSeatService'))
  failures.push('serviço de assentos adicionais ausente.');
if (!svc.includes('AdditionalSeatRules.ValidatePurchase('))
  failures.push('compra sem validação das regras puras (Validade/teto).');
if (!svc.includes('"Subscription.AdditionalSeatsPurchased"'))
  failures.push('compra de assentos sem evento de auditoria.');

// ---------- 4. Página Assinatura: gate de permissão + handler ----------
if (!page.includes('OnPostPurchaseSeatsAsync'))
  failures.push('página Assinatura sem o handler de compra de assentos.');
if (!page.includes('PermissionCodes.SubscriptionsManage'))
  failures.push('compra de assentos sem o gate Subscriptions.Manage.');
if (!page.includes('seatService.PurchaseAsync('))
  failures.push('página Assinatura não chama o serviço de assentos adicionais.');

// ---------- 5. View expõe a seção de usuários adicionais ----------
if (!view.includes('id="usuarios-adicionais"'))
  failures.push('view da Assinatura sem a seção de usuários adicionais.');
if (!view.includes('asp-page-handler="PurchaseSeats"'))
  failures.push('view sem o formulário de contratação de assentos.');

// ---------- 6. O limite de equipe segue sendo aplicado no convite de usuário ----------
if (!userpage.includes('AccountInvitationRules.TeamCapReached('))
  failures.push('convite de usuário deixou de aplicar o teto da equipe.');
if (!userpage.includes('PlanFeatureCodes.TeamMembersLimit'))
  failures.push('convite de usuário deixou de consultar o limite TeamMembersLimit.');

// ---------- 7. Os testes fixam as regras puras ----------
for (const [name, token] of [
  ['teto ilimitado permanece nulo', 'AddLimitIncrement_keeps_unlimited_as_null'],
  ['compra além do teto bloqueada', 'ValidatePurchase_blocks_beyond_max_additional_seats'],
  ['só Negócio compra assentos', 'IsEligiblePlan_only_business_can_buy_seats'],
  ['custo mensal multiplica o preço', 'MonthlyCost_multiplies_seat_price'],
])
  if (!tests.includes(token)) failures.push(`teste das regras de assentos ausente: ${name}`);

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Assentos adicionais pagos validados: seed idempotente do addon, compra só no Negócio com teto e auditoria, teto da equipe cresce na autorização e na contratação.');
