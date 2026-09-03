import { access, readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { runSprint56Check } from './sprint56-ui-checks.mjs';

const web = 'src/OrcaFacil.Web';
const read = file => readFile(path.join(web, file), 'utf8');
const exists = file => access(path.join(web, file)).then(() => true, () => false);
async function walk(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  return (await Promise.all(entries.map(entry => entry.isDirectory() ? walk(path.join(dir, entry.name)) : [path.join(dir, entry.name)]))).flat();
}
const requireText = (source, values, label) => values.flatMap(value => source.includes(value) ? [] : [`${label}: contrato ausente: ${value}`]);
const priorModes = new Set(['form-validation-ui','validation-messages','popup-feedback','toast-host','confirm-dialogs','form-quality','button-quality','link-quality','empty-states','loading-states','mobile-layout','dashboard-premium','documents-new-premium','commercial-routine-premium','portal-design','admin-design','system-health-design','no-technical-id-inputs','js-safety']);

export async function runSprint57Check(mode) {
  if (mode === 'ui-total-v59' || mode === 'design-system-v59') await runSprint56Check(mode === 'ui-total-v59' ? 'ui-total-v58' : 'design-system-v58');
  else if (priorModes.has(mode)) await runSprint56Check(mode);

  const failures = [];
  const files = (await walk(path.join(web, 'Pages'))).filter(file => file.endsWith('.cshtml'));
  const pages = Object.fromEntries(await Promise.all(files.map(async file => [file, await readFile(file, 'utf8')])));
  const allPages = Object.values(pages).join('\n');
  const layout = await read('Pages/Shared/_Layout.cshtml');
  const publicLayout = await read('Pages/Shared/_PublicLayout.cshtml');
  const feedback = await read('wwwroot/css/feedback.css');
  const dialogs = await read('wwwroot/js/dialogs.js');
  const forms = await read('wwwroot/js/forms.js');

  if (['ui-total-v59','design-system-v59'].includes(mode)) {
    for (const file of ['wwwroot/js/feedback.js','wwwroot/js/dialogs.js','wwwroot/js/forms.js','Pages/Shared/_ToastHost.cshtml','Pages/Shared/_ConfirmDialog.cshtml']) if (!await exists(file)) failures.push(`V5.9: arquivo ausente: ${file}`);
    failures.push(...requireText(layout + publicLayout, ['_ToastHost','_ConfirmDialog','~/js/feedback.js','~/js/dialogs.js','~/js/forms.js'], 'Layouts V5.9'));
    failures.push(...requireText(feedback, ['safe-area-inset-top','is-loading','prefers-reduced-motion'], 'Feedback responsivo'));
  }
  if (['ui-total-v59','page-standard-v59'].includes(mode)) {
    for (const file of ['Pages/Dashboard/Index.cshtml','Pages/Documents/New.cshtml','Pages/CommercialRoutine/Index.cshtml']) {
      const source = await read(file);
      failures.push(...requireText(source, ['<h1','of-'], file));
    }
  }
  if (['ui-total-v59','critical-actions-confirmation'].includes(mode) && !/data-confirm\s*=/.test(allPages)) failures.push('Nenhuma ação crítica usa data-confirm.');
  if (['ui-total-v59','accessibility-basic'].includes(mode)) failures.push(...requireText(dialogs + layout + publicLayout, ['event.key !== \'Tab\'','aria-modal="true"','of-skip-link'], 'Acessibilidade'));
  if (['ui-total-v59','form-quality'].includes(mode)) failures.push(...requireText(forms, ['data-submit-lock','form.checkValidity()','aria-busy','event.submitter'], 'Submit seguro'));
  if (failures.length) { console.error(`Sprint 57 (${mode}):\n- ${failures.join('\n- ')}`); process.exitCode = 1; return; }
  console.log(`Sprint 57 (${mode}): ${files.length} telas e contratos V5.9 validados.`);
}
