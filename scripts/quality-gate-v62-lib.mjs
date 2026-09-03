import { existsSync, readFileSync } from 'node:fs';

const read = path => readFileSync(path, 'utf8');
const requireFile = path => { if (!existsSync(path)) throw new Error(`arquivo obrigatório ausente: ${path}`); return read(path); };
const requireTokens = (path, tokens) => { const text = requireFile(path).toLowerCase(); for (const token of tokens) if (!text.includes(token.toLowerCase())) throw new Error(`${path} não contém ${token}`); };

export function checkV62(mode = 'all') {
  const checks = {
    schema() {
      const sqlFiles = ['database/hotfix_schema_drift_quality_gate_v62.sql','database/script_completop.sql','database/patch_release_candidate_schema.sql'];
      const budget = ['title','profession','is_system_template','is_active','is_deleted','created_at','updated_at','deleted_at','deleted_by'];
      for (const file of sqlFiles) requireTokens(file, ['orcafacil.budget_templates', ...budget.slice(0, 6)]);
      const hotfix = requireFile(sqlFiles[0]).toLowerCase();
      if (/\bdrop\s+(table|column)\b/.test(hotfix)) throw new Error('hotfix V6.2 contém operação destrutiva');
      requireTokens('src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs', ['qualitygateschemadriftv62migration','budget_template_items','email_outbox_messages']);
    },
    routes() {
      const pages = ['Index','Auth/Login','Auth/Register','Auth/ForgotPassword','Onboarding/Index','Dashboard/Index','Clients/Index','Documents/Index','Documents/New','CommercialRoutine/Index','Diagnostico','Admin/Index','Admin/QualityGate'];
      for (const page of pages) requireFile(`src/OrcaFacil.Web/Pages/${page}.cshtml`);
      requireTokens('src/OrcaFacil.Application/Quality/QualityGateService.cs', ['CriticalRoutes','CheckRegistrationContractAsync','source.blockers','Portal/Index','PartnerPortal/Index']);
    },
    ui() {
      requireTokens('src/OrcaFacil.Web/Pages/Admin/QualityGate.cshtml', ['<h1>Quality Gate</h1>','aria-live','quality-gate-card','loading-state','Próxima ação recomendada']);
      requireTokens('src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml', ['_ToastHost','_ConfirmDialog','feedback.js','dialogs.js','forms.js']);
      requireTokens('src/OrcaFacil.Web/wwwroot/css/admin.css', ['quality-gate-page','@media(max-width:25rem)','prefers-reduced-motion']);
    }
  };
  if (mode === 'all') Object.values(checks).forEach(check => check());
  else if (checks[mode]) checks[mode]();
  else throw new Error(`modo desconhecido: ${mode}`);
  console.log(`Quality Gate V6.2 (${mode}): OK`);
}
