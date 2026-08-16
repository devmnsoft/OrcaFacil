import { readFile } from 'node:fs/promises';

const required = [
  'account_onboarding_states',
  'ix_account_onboarding_states_account_id_user_id',
  'ix_account_onboarding_states_current_step_last_seen_at',
  'failed_login_attempts',
  'session_version'
];
const files = [
  'database/script_completop.sql',
  'database/patch_release_candidate_schema.sql',
  'database/patch_fix_account_onboarding_states.sql'
];
let failed = false;
for (const file of files) {
  const sql = (await readFile(file, 'utf8')).toLowerCase();
  const expected = file.endsWith('patch_fix_account_onboarding_states.sql')
    ? required.slice(0, 3)
    : required;
  const missing = expected.filter(token => !sql.includes(token));
  if (missing.length) {
    failed = true;
    console.error(`${file}: ausente: ${missing.join(', ')}`);
  } else {
    console.log(`${file}: contrato de schema OK`);
  }
}
if (failed) process.exitCode = 1;
