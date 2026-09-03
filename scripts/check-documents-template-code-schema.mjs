import { readFileSync } from 'node:fs';
const files = ['database/hotfix_documents_full_schema_drift_v61.sql','database/script_completop.sql','database/patch_release_candidate_schema.sql'];
for (const file of files) {
  const sql = readFileSync(file, 'utf8').toLowerCase();
  if (!/add column if not exists template_code varchar\(40\)/.test(sql)) throw new Error(`${file}: documents.template_code ausente ou incompatível`);
  if (!sql.includes('alter column template_code set not null')) throw new Error(`${file}: template_code não preserva o contrato obrigatório do EF`);
}
const mapping = readFileSync('src/OrcaFacil.Persistence/Configurations/DocumentConfiguration.cs','utf8');
if (!mapping.includes('HasColumnName("template_code")') || !mapping.includes('x.TemplateCode')) throw new Error('mapeamento EF de TemplateCode ausente');
console.log('documents.template_code schema: OK');
