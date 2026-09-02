import { readFileSync } from 'node:fs';
for (const file of ['database/script_completop.sql','database/patch_release_candidate_schema.sql','database/hotfix_documents_commercial_columns.sql','src/OrcaFacil.Persistence/Migrations/20260902020000_FixDocumentsCommercialSchemaDepositAmount.cs']) {
  const source=readFileSync(file,'utf8');
  if (!/ADD COLUMN IF NOT EXISTS deposit_amount numeric\(18,2\)/i.test(source)) throw new Error(`${file}: reparo nullable numeric(18,2) de deposit_amount ausente`);
  if (/deposit_amount numeric\(18,2\) NOT NULL/i.test(source)) throw new Error(`${file}: deposit_amount deve acompanhar decimal? do EF`);
}
console.log('documents.deposit_amount schema: OK');
