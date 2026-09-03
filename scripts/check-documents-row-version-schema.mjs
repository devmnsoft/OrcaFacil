import { readFileSync } from 'node:fs';
const files = ['database/hotfix_documents_row_version_schema_drift_v60.sql','database/script_completop.sql','database/patch_release_candidate_schema.sql'];
for (const file of files) {
  const sql = readFileSync(file, 'utf8').toLowerCase();
  if (!sql.includes('add column if not exists row_version bytea')) throw new Error(`${file}: documents.row_version bytea ausente`);
}
const entity = readFileSync('src/OrcaFacil.Domain/Entities/Document.cs','utf8');
const mapping = readFileSync('src/OrcaFacil.Persistence/Configurations/DocumentConfiguration.cs','utf8');
const context = readFileSync('src/OrcaFacil.Persistence/OrcaFacilDbContext.cs','utf8');
if (!entity.includes('byte[] RowVersion') || !mapping.includes('IsConcurrencyToken') || !mapping.includes('HasColumnType("bytea")')) throw new Error('Contrato EF de RowVersion inválido');
if (!context.includes('RefreshDocumentConcurrencyTokens')) throw new Error('RowVersion não é renovado nas atualizações');
console.log('documents row-version schema and concurrency: OK');
