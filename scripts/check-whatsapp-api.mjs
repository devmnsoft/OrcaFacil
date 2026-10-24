import fs from 'node:fs';

// Valida o contrato da integração oficial WhatsApp Cloud API (V7.7), item 4 do
// backlog de evolução. Prova que o canal é real e honesto: envio pela Graph API
// (nada de wa.me/simulação), assinatura HMAC-SHA256 verificada em tempo constante,
// deduplicação por CONTEÚDO (o entry.id do Meta é constante por conta e os
// callbacks de status de uma mesma mensagem compartilham o id), credenciais
// protegidas em repouso e nunca devolvidas à tela, opt-out respeitado e o schema
// encadeado na base nova.

const fail = (m) => { throw new Error(`check:whatsapp-api: ${m}`); };
const read = (p) => fs.readFileSync(p, 'utf8');
const stripComments = (s) => s.replace(/\/\/[^\n]*/g, '');

const src = 'src/OrcaFacil.Web/Services';
const signature = read('src/OrcaFacil.Application/Omnichannel/WhatsAppSignature.cs');
const eventKey = read('src/OrcaFacil.Application/Omnichannel/WhatsAppWebhookEventKey.cs');
const graph = read('src/OrcaFacil.Infrastructure/WhatsApp/WhatsAppGraphClient.cs');
const processor = read(`${src}/WhatsAppWebhookProcessor.cs`);
const sendService = read(`${src}/OmnichannelWhatsAppApiService.cs`);
const entity = read('src/OrcaFacil.Domain/Entities/OmnichannelWhatsAppAccount.cs');
const config = read('src/OrcaFacil.Persistence/Configurations/OmnichannelConfiguration.cs');
const program = read('src/OrcaFacil.Web/Program.cs');
const pageCs = read('src/OrcaFacil.Web/Pages/Settings/Integrations/Index.cshtml.cs');
const pageCshtml = read('src/OrcaFacil.Web/Pages/Settings/Integrations/Index.cshtml');
const migration = read('src/OrcaFacil.Persistence/Migrations/20261021165111_AddOmnichannelWhatsAppApi.cs');
const snapshot = read('src/OrcaFacil.Persistence/Migrations/OrcaFacilDbContextModelSnapshot.cs');
const hotfix = read('database/hotfix_omnichannel_whatsapp_api_v77.sql');
const consolidated = read('database/script_completop.sql');
const tests = read('tests/OrcaFacil.UnitTests/WhatsAppCloudApiContractTests.cs');

// ---------- 1. Envio real: Graph API, sem simulação ----------
for (const token of ['graph.facebook.com', 'messaging_product', 'Bearer', 'WhatsAppSendResult'])
  if (!graph.includes(token)) fail(`cliente sem contrato real: ${token}`);
if (graph.includes('wa.me')) fail('o envio oficial não pode usar wa.me (link preparado, não envio).');
if (!graph.includes('IsSuccessStatusCode')) fail('resposta não-2xx não está tratada como falha.');
if (!graph.includes('Resposta da Cloud API sem id de mensagem'))
  fail('2xx sem id de mensagem não está recusado (seria falso "entregue").');
if (!sendService.includes('result.Succeeded ? OmnichannelMessageStatus.Queued : OmnichannelMessageStatus.Failed'))
  fail('o registro da mensagem não reflete o resultado real do provedor.');
if (!sendService.includes('WhatsAppGraphClient') && !sendService.includes('IWhatsAppGraphClient'))
  fail('o serviço de envio não chama o cliente real da Graph API.');

// ---------- 2. Assinatura HMAC em tempo constante ----------
for (const token of ['HMACSHA256', 'FixedTimeEquals', 'sha256=', 'X-Hub-Signature-256'])
  if (!signature.includes(token)) fail(`assinatura incompleta: ${token}`);
if (!processor.includes('WhatsAppSignature.Verify')) fail('o webhook não verifica a assinatura antes de aplicar.');
if (!processor.includes('Assinatura do webhook inválida')) fail('assinatura inválida não é recusada com erro explícito.');

// ---------- 3. Deduplicação por conteúdo ----------
if (!eventKey.includes('withStatus')) fail('chave de dedup sem considerar o estado do status (sent/delivered/read).');
if (!eventKey.includes('return Hash(rawBody)')) fail('chave sem fallback determinístico para payload sem ids.');
if (!processor.includes('DedupColumn')) fail('dedup não limitada a event_type varchar(40).');
if (processor.includes('EventType = dedupKey')) fail('chave bruta ainda é gravada em event_type (varchar 40).');

// ---------- 4. Credenciais protegidas e nunca devolvidas à tela ----------
// O propósito tem de ser o mesmo onde se grava e onde se lê, senão o Unprotect lança.
const protectorPurpose = 'OrcaFacil.Omnichannel.WhatsApp.v1';
const usesPurpose = (file) =>
  file.includes(`CreateProtector("${protectorPurpose}")`) ||           // literal
  (file.includes('CreateProtector(ProtectorPurpose)') &&               // constante
   file.includes(`"${protectorPurpose}"`));                            // cujo valor bate

for (const [name, file] of [['página', pageCs], ['serviço de envio', sendService], ['processador', processor]])
  if (!usesPurpose(file)) fail(`propósito de proteção ausente/divergente em ${name}.`);
if (!entity.includes('ProtectedAccessToken') || !entity.includes('ProtectedAppSecret'))
  fail('entidade sem credenciais protegidas.');
if (/AccessToken\s*=\s*Input\.|AppSecret\s*=\s*Input\./.test(pageCs))
  fail('credencial está sendo devolvida ao modelo da tela.');
if (!pageCs.includes('WhatsAppAccessTokenConfigured') || !pageCs.includes('WhatsAppAppSecretConfigured'))
  fail('tela sem indicador de "já salvo" (sem ele o operador reenvia o segredo à toa).');
if (!pageCs.includes('hasToken && hasSecret')) fail('habilitar sem exigir token + app secret juntos.');

// ---------- 5. Nenhuma simulação de envio ----------
for (const [name, file] of [['serviço de envio', sendService], ['processador', processor], ['página', pageCs]]) {
  const code = stripComments(file);
  const claimsSimulation = /\bsimulad[oa]s?\b/i.test(code) || /\bsimular\b/i.test(code);
  const deniesIt = /n[aã]o\s+(e[áa]\s+)?simulad|nada\s+[ée]\s+simulad|nenhum\s+envio\s+de\s+teste/i.test(code);
  if (claimsSimulation && !deniesIt) fail(`${name}: menção de simulação sem negação explícita.`);
}

// ---------- 6. Webhook: handshake restrito, POST assinado ----------
if (!program.includes('MapGet("/api/webhooks/whatsapp"')) fail('handshake GET do Meta ausente.');
if (!program.includes('MapPost("/api/webhooks/whatsapp"')) fail('endpoint POST do webhook ausente.');
if (!program.includes('AllowAnonymous')) fail('webhooks precisam ser anônimos (o Meta não tem sessão).');
if (!processor.includes('x.WebhookVerifyToken == token && x.IsEnabled'))
  fail('handshake aceita verify token de conta desabilitada.');
if (!processor.includes('Número de telefone não pertence a nenhuma conta habilitada'))
  fail('POST não rejeita phone_number_id sem conta habilitada.');

// ---------- 7. Opt-out respeitado antes do envio ----------
if (!sendService.includes('OmnichannelOptOutEvents')) fail('envio sem checagem de opt-out.');
if (!sendService.includes('"Commercial"')) fail('opt-out sem escopo comercial.');

// ---------- 8. Schema: migration, snapshot, hotfix e base nova ----------
for (const token of ['omnichannel_whatsapp_accounts', 'phone_number_id', 'protected_access_token', 'protected_app_secret'])
  if (!migration.includes(token)) fail(`migration sem ${token}`);
if (!snapshot.includes('omnichannel_whatsapp_accounts')) fail('snapshot desatualizado (sem a tabela).');
if (!hotfix.includes('ADD COLUMN IF NOT EXISTS phone_number_id')) fail('hotfix não é aditivo/idempotente.');
if (!consolidated.includes('\\ir hotfix_omnichannel_whatsapp_api_v77.sql'))
  fail('hotfix não encadeado no script_completop.sql (base nova nasceria sem as colunas tipadas).');
if (!config.includes('omnichannel_whatsapp_accounts')) fail('configuração EF sem a tabela.');
for (const service of ['OmnichannelWhatsAppApiService', 'IWhatsAppWebhookProcessor', 'IWhatsAppGraphClient'])
  if (!program.includes(service)) fail(`${service} sem registro de DI.`);

// ---------- 9. Tela de configuração presente e auditada ----------
if (!pageCshtml.includes('WhatsApp Cloud API')) fail('seção de configuração ausente em Settings/Integrations.');
if (!pageCshtml.includes('asp-page-handler="WhatsApp"')) fail('form do WhatsApp sem handler próprio.');
if (!pageCs.includes('integrations.whatsapp_account.saved')) fail('salvamento sem auditoria.');
// A URL é montada no PageModel (baseada na PublicBaseUrl) e exibida na view.
if (!pageCs.includes('/api/webhooks/whatsapp') || !pageCshtml.includes('WhatsAppWebhookUrl'))
  fail('URL do webhook não é calculada nem exibida para colar no app da Meta.');

// ---------- 10. Testes cobrem as regressões que já doeram ----------
for (const [name, token] of [
  ['dedup por conteúdo (entry.id constante)', 'Duas_mensagens_diferentes_com_mesmo_entry_id_tem_chaves_diferentes'],
  ['avanço de status (sent→delivered→read)', 'Callback_de_status_tambem_entra_na_chave_com_o_estado'],
  ['cliente sem simulação de sucesso', 'Status_nao_2xx_vira_falha_e_nunca_sucesso'],
  ['telefone com DDI', 'Telefone_e_normalizado_para_e164_com_ddi_brasil'],
])
  if (!tests.includes(token)) fail(`teste ausente: ${name}.`);

console.log('WhatsApp Cloud API (V7.7): envio real pela Graph API, assinatura HMAC em tempo constante, ' +
  'deduplicação por conteúdo (id + estado de status), credenciais protegidas e fora da tela, ' +
  'opt-out, migration + hotfix encadeado na base nova e configuração auditada OK.');
