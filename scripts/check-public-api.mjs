import fs from 'node:fs';

// Valida o contrato da API pública V1 (rotas base, tenant/scopes, hash somente da chave, idempotência,
// registro no host) E a leitura de recursos de negócio adicionada nesta evolução: orçamentos
// (DocumentType.Budget), ordens de serviço, recibos e contratos recorrentes — cada um com rota de LISTA
// paginada e DETALHE por id, ambos isolados por conta e cobertos pelo escopo já declarado em ApiKeyService.
// Todo escopo exigido por um endpoint deve estar no conjunto AllowedScopes (fonte da verdade). Coleta falhas.

const read = (p) => fs.readFileSync(p, 'utf8');
const api = read('src/OrcaFacil.Web/Api/PublicApiV1.cs');
const auth = read('src/OrcaFacil.Web/Api/ApiKeyAuthenticationHandler.cs');
const entities = read('src/OrcaFacil.Domain/Entities/Integrations.cs');
const program = read('src/OrcaFacil.Web/Program.cs');
const keys = read('src/OrcaFacil.Application/Integrations/ApiKeyService.cs');
const failures = [];
const fail = (m) => failures.push(m);

// ---- Contrato V1: rotas base, tenant, hash da chave, persistência operacional e registro ----
const required = ['/api/v1', '/clients', '/services', '/webhooks/events', 'clients.read', 'clients.write', 'services.read', 'Idempotency-Key', 'scope_required', 'account_id'];
for (const x of required) if (!api.includes(x)) fail(`contrato V1 incompleto: ${x}`);
if (!auth.includes('SHA256.HashData') || auth.includes('PlaintextKey =')) fail('autenticação não preserva a chave somente em hash');
if (!entities.includes('ApiRequestLog') || !entities.includes('ApiIdempotencyKey')) fail('persistência operacional ausente (ApiRequestLog/ApiIdempotencyKey)');
if (!program.includes('MapPublicApiV1') || !program.includes('ApiKeyAuthenticationHandler')) fail('API não registrada no host');

// ---- Leitura de recursos de negócio: lista+detalhe por recurso, com escopo declarado ----
const init = keys.match(/AllowedScopes\s*=\s*new[\s\S]*?;/)?.[0] || '';
const allowed = new Set([...init.matchAll(/"([a-z0-9_]+\.[a-z0-9_]+)"/g)].map((m) => m[1]));
allowed.add('profile.read'); // concedido implicitamente pelo ApiScopeFilter; não é escopo selecionável de chave

const RESOURCES = [
  { route: 'quotes', scope: 'quotes.read' },
  { route: 'work-orders', scope: 'work_orders.read' },
  { route: 'receipts', scope: 'receipts.read' },
  { route: 'contracts', scope: 'contracts.read' },
];
for (const { route, scope } of RESOURCES) {
  if (!api.includes(`MapGet("/${route}",`)) fail(`${route}: rota de lista ausente`);
  if (!api.includes('"/' + route + '/{id:guid}"')) fail(`${route}: rota de detalhe ausente`);
  if (!api.includes(`new RequiredScope("${scope}")`)) fail(`${route}: escopo ${scope} não exigido em nenhum endpoint`);
  if (!allowed.has(scope)) fail(`${route}: escopo ${scope} removido do AllowedScopes`);
}
// Consistência: todo escopo efetivamente usado em um endpoint deve estar permitido (inclui profile.read implícito).
const used = new Set([...api.matchAll(/RequiredScope\("([a-z0-9_]+\.[a-z0-9_]+)"\)/g)].map((m) => m[1]));
for (const s of used) if (!allowed.has(s)) fail(`escopo exigido mas não permitido: ${s}`);

// Cada recurso consulta sua tabela (AsNoTracking) e orçamentos filtram apenas DocumentType.Budget.
for (const ds of ['db.Documents.AsNoTracking()', 'db.WorkOrders.AsNoTracking()', 'db.Receipts.AsNoTracking()', 'db.RecurringContracts.AsNoTracking()'])
  if (!api.includes(ds)) fail(`query de recurso ausente/sem AsNoTracking: ${ds}`);
if ((api.match(/DocumentType\.Budget/g) || []).length < 2) fail('orçamentos não filtram apenas DocumentType.Budget (lista e detalhe)');

// Isolamento por conta em todas as queries + paginação/resposta/erro 404 consistentes (6 listas, 6 detalhes).
if ((api.match(/Account\(user\)/g) || []).length < 12) fail('isolamento por conta fraco: poucas ocorrências de Account(user) nas queries');
if ((api.match(/Math\.Clamp\(pageSize/g) || []).length < 6) fail('paginação ausente: Math.Clamp(pageSize) em menos de 6 listas');
if ((api.match(/Results\.Ok\(Page\(/g) || []).length < 6) fail('resposta paginada ausente: Page( em menos de 6 listas');
if ((api.match(/ApiError\(404,\s*"not_found"/g) || []).length < 6) fail('detalhe sem erro 404 consistente: ApiError(404 em menos de 6 rotas');

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('API pública validada: contrato V1 (tenant, scopes, hash da chave, idempotência, registro) + leitura de orçamentos, ordens, recibos e contratos (lista+detalhe, isolados por conta, paginados, escopos declarados).');
