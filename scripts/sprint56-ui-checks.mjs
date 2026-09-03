import { access, readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const web = 'src/OrcaFacil.Web';
const read = file => readFile(path.join(web, file), 'utf8');
const exists = file => access(path.join(web, file)).then(() => true, () => false);
async function walk(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  return (await Promise.all(entries.map(entry => entry.isDirectory() ? walk(path.join(dir, entry.name)) : [path.join(dir, entry.name)]))).flat();
}
const required = (source, values, label) => values.flatMap(value => source.includes(value) ? [] : [`${label}: contrato ausente: ${value}`]);
const critical = ['Pages/Auth/Login.cshtml','Pages/Auth/Register.cshtml','Pages/Onboarding/Index.cshtml','Pages/Dashboard/Index.cshtml','Pages/Clients/Index.cshtml','Pages/Clients/Details.cshtml','Pages/Documents/New.cshtml','Pages/Documents/Index.cshtml','Pages/CommercialRoutine/Index.cshtml'];

export async function runSprint56Check(mode) {
  const failures = [];
  const files = await walk(path.join(web, 'Pages'));
  const razorFiles = files.filter(file => file.endsWith('.cshtml'));
  const razor = Object.fromEntries(await Promise.all(razorFiles.map(async file => [file, await readFile(file, 'utf8')])));
  const criticalSource = (await Promise.all(critical.map(read))).join('\n');
  const tokens = await read('wwwroot/css/tokens.css');
  const design = await read('wwwroot/css/design-system.css');
  const forms = await read('wwwroot/css/forms.css');
  const feedback = await read('wwwroot/css/feedback.css');
  const overlay = await read('Pages/Shared/Partials/_OverlayHost.cshtml');
  const feedbackJs = await read('wwwroot/js/ui/feedback.js');
  const overlayJs = await read('wwwroot/js/ui/overlay-manager.js');
  const layout = await read('Pages/Shared/_Layout.cshtml');
  const toastHost = await read('Pages/Shared/Partials/_ToastHost.cshtml');

  if (['ui-total-v58','design-system-v58'].includes(mode)) {
    for (const name of ['tokens','design-system','base','components','forms','tables','navigation','dashboard','auth','commercial','documents','portals','admin','mobile','responsive','feedback','overlays']) if (!await exists(`wwwroot/css/${name}.css`)) failures.push(`CSS obrigatório ausente: ${name}.css`);
    failures.push(...required(tokens, ['Design System V5.8','--of-info','--of-danger','--of-focus-ring','--of-shadow-premium','--of-z-dialog','--of-breakpoint-md'], 'tokens.css'));
    failures.push(...required(design, ['page-shell','page-hero','section-header','metric-card','summary-card','action-card','status-badge','priority-badge','risk-badge','validation-summary','field-validation','form-panel','form-grid','form-section','form-actions','input-help','drawer','alert','empty-state','loading-state','premium-table','responsive-table-card','filter-bar','action-bar','breadcrumb','tabs','wizard-steps','timeline','kanban-card','quick-actions'], 'design-system.css'));
  }
  if (['ui-total-v58','button-quality','link-quality','form-quality'].includes(mode)) for (const [file, source] of Object.entries(razor)) {
    if (/href\s*=\s*["'](?:\s*|#|javascript:void[^"']*)["']/i.test(source)) failures.push(`${file}: link vazio ou comando usado como URL`);
    for (const match of source.matchAll(/<button\b([^>]*)>/gi)) if (!/\btype\s*=/.test(match[1])) failures.push(`${file}: button sem type`);
    if (/<form\b[^>]*method\s*=\s*["']post["']/i.test(source) && !/(AntiForgeryToken|asp-page|asp-page-handler)/.test(source)) failures.push(`${file}: POST sem antiforgery/tag helper`);
  }
  if (['ui-total-v58','form-validation-ui','form-quality'].includes(mode)) failures.push(...required((await read('Pages/Auth/Login.cshtml')) + await read('Pages/CommercialRoutine/Index.cshtml') + forms, ['asp-validation-summary','asp-validation-for','AntiForgeryToken','data-submit-lock','data-loading','input-validation-error','aria-invalid','of-field-error'], 'Formulários críticos'));
  if (['ui-total-v58','validation-messages'].includes(mode)) failures.push(...required(await read('Services/UserFeedbackMessageCatalog.cs'), ['ReviewHighlightedFields','ClientRequired','ServiceRequired','TemporaryError','Código de atendimento','logger.LogError'], 'Catálogo de mensagens'));
  if (['ui-total-v58','popup-feedback','toast-host','confirm-dialogs'].includes(mode)) {
    failures.push(...required(layout + toastHost + feedback + overlay + feedbackJs + overlayJs, ['data-toast-host','aria-live="polite"','id="confirm-dialog"','role="dialog"','aria-modal="true"','data-confirm-accept','Escape','focusableSelector','overlay:close'], 'Feedback acessível'));
    if (/\b(?:window\.)?(?:alert|confirm)\s*\(/.test(feedbackJs)) failures.push('Feedback usa diálogo nativo.');
  }
  if (['ui-total-v58','empty-states'].includes(mode)) failures.push(...required(criticalSource, ['_EmptyState','of-empty-state'], 'Estados vazios'));
  if (['ui-total-v58','loading-states'].includes(mode)) failures.push(...required(criticalSource + design, ['data-loading','loading-state'], 'Estados de carregamento'));
  if (mode === 'documents-new-premium') failures.push(...required(await read('Pages/Documents/New.cshtml') + await read('wwwroot/js/budget-start.js'), ['<h1','of-start-options','data-scroll-target','getElementById','of-start-empty','data-service-picker'], 'Documents/New'));
  if (mode === 'commercial-routine-premium') failures.push(...required(await read('Pages/CommercialRoutine/Index.cshtml'), ['<h1','of-routine-summary','of-filter-bar','of-empty-state','AntiForgeryToken','asp-validation-summary','data-confirm','data-submit-lock'], 'Rotina Comercial'));
  if (['portal-design','admin-design','system-health-design','mobile-layout','accessibility-basic','dashboard-premium','no-technical-id-inputs','js-safety'].includes(mode)) {
    const legacy = await import('./sprint55-design-checks.mjs'); await legacy.runSprint55Check(mode);
  }
  if (failures.length) { console.error(`Sprint 56 (${mode}):\n- ${failures.join('\n- ')}`); process.exitCode = 1; return; }
  console.log(`Sprint 56 (${mode}): ${razorFiles.length} telas e contratos V5.8 validados.`);
}
