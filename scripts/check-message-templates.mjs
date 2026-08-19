import { existsSync, readFileSync } from 'node:fs';

const failures = [];
const paths = {
  entity: 'src/OrcaFacil.Domain/Entities/CommercialInteraction.cs',
  service: 'src/OrcaFacil.Web/Services/CommercialAutomationService.cs',
  page: 'src/OrcaFacil.Web/Pages/MessageTemplates/Index.cshtml',
  model: 'src/OrcaFacil.Web/Pages/MessageTemplates/Index.cshtml.cs',
  script: 'src/OrcaFacil.Web/wwwroot/js/message-templates.js'
};
const source = {};
for (const [key, path] of Object.entries(paths)) {
  if (!existsSync(path)) failures.push(`${path}: arquivo obrigatório ausente`);
  else source[key] = readFileSync(path, 'utf8');
}

const requiredVariables = ['ClienteNome', 'EmpresaNome', 'NumeroOrcamento', 'ValorTotal', 'Validade', 'LinkPublico', 'NomeUsuario', 'TelefoneEmpresa'];
for (const variable of requiredVariables) {
  if (source.service && !source.service.includes(`"${variable}"`)) failures.push(`${paths.service}: variável permitida ausente: ${variable}`);
}

const contracts = [
  ['entity', /class CommercialMessageTemplate[\s\S]*?AccountId[\s\S]*?IsActive[\s\S]*?IsSystem/, 'entidade não protege escopo, ativação e modelos do sistema'],
  ['service', /EnsureAccountAccessAsync/, 'serviço não valida acesso à conta'],
  ['service', /x\.AccountId == AccountId/, 'leitura ou alteração sem isolamento por conta'],
  ['service', /VariableRegex\(\).*?Variables\.Contains/s, 'variáveis não são validadas no servidor'],
  ['service', /channel is not \("WhatsApp" or "Email" or "General"\)/, 'canal não é validado no servidor'],
  ['service', /!x\.IsSystem/, 'template do sistema pode ser alterado'],
  ['service', /MESSAGE_TEMPLATE_(?:CREATED|UPDATED)/, 'alteração não gera atividade auditável'],
  ['model', /OnPostSaveAsync/, 'handler de persistência ausente'],
  ['page', /method="post"[\s\S]*?asp-page-handler="Save"/, 'formulário de persistência ausente'],
  ['page', /data-template-workspace/, 'workspace de preview ausente'],
  ['script', /safeInit/, 'JavaScript não usa inicialização isolada'],
  ['script', /encodeURIComponent|URLSearchParams/, 'mensagem não possui codificação segura para compartilhamento ou preview']
];
for (const [key, pattern, message] of contracts) {
  if (source[key] && !pattern.test(source[key])) failures.push(`${paths[key]}: ${message}`);
}

if (source.page && /@(?:Model\.)?(?:Token|PublicToken)\b/.test(source.page)) failures.push(`${paths.page}: token público bruto renderizado`);
if (failures.length) {
  console.error(`Templates de mensagem reprovados:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log(`Templates de mensagem: ${requiredVariables.length} variáveis, tenant, persistência, auditoria e preview validados.`);
