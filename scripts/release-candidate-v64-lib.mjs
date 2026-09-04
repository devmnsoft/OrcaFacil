import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const read = file => fs.readFileSync(path.join(root, file), 'utf8');
const requireFile = file => { if (!fs.existsSync(path.join(root, file))) throw new Error(`Arquivo obrigatório ausente: ${file}`); return read(file); };
const requireText = (file, values) => { const source = requireFile(file); for (const value of values) if (!source.includes(value)) throw new Error(`${file}: contrato ausente: ${value}`); };

const checks = {
  schema() {
    requireText('src/OrcaFacil.Persistence/Diagnostics/DatabaseDiagnosticsService.cs', ['public_document_decisions', 'MissingTable', 'MissingColumn', 'IncompatibleType', 'MissingIndex']);
    requireText('src/OrcaFacil.Application/Abstractions/IDatabaseDiagnosticsService.cs', ['ImpactedRoutes', 'RecommendedPatch']);
    requireText('database/hotfix_release_candidate_schema_v64.sql', ['information_schema', 'public_document_decisions', 'COMMIT']);
    requireText('database/patch_release_candidate_schema.sql', ['hotfix_release_candidate_schema_v64.sql']);
    requireText('database/script_completop.sql', ['Release Candidate V6.4']);
  },
  documents() {
    requireText('src/OrcaFacil.Persistence/Services/GuidedBudgetStartService.cs', ['x.AccountId == accountId', 'x.IsSystemTemplate', 'x.UserId == userId', 'x.IsActive', '!x.IsDeleted']);
    requireText('src/OrcaFacil.Web/Pages/Documents/New.cshtml', ['data-picker-search="clients"', 'data-picker-search="services"', 'Nenhum modelo ativo', 'type="submit"']);
  },
  commercial() {
    requireText('src/OrcaFacil.Web/Services/CommercialAutomationService.cs', ['AccountId']);
    requireFile('src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml');
  },
  dashboard() {
    const source = requireFile('src/OrcaFacil.Web/Services/DashboardExperienceService.cs');
    if (/Math\.random/i.test(source)) throw new Error('Dashboard contém KPI aleatório.');
    requireText('src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml', ['of-metrics-empty']);
  },
  demo() {
    requireText('database/seed_demo_release_candidate_v64.sql', ['Demo', 'ON CONFLICT', 'current_setting']);
    requireText('scripts/seed-demo-release-candidate.ps1', ['Development', 'DEMO_SEED_ENABLED']);
  },
  ui() {
    for (const file of ['src/OrcaFacil.Web/Pages/Shared/_ToastHost.cshtml','src/OrcaFacil.Web/Pages/Shared/_ConfirmDialog.cshtml','src/OrcaFacil.Web/wwwroot/js/feedback.js','src/OrcaFacil.Web/wwwroot/js/dialogs.js','src/OrcaFacil.Web/wwwroot/js/forms.js','src/OrcaFacil.Web/wwwroot/css/feedback.css']) requireFile(file);
  }
};

export function checkV64(scope = 'all') {
  const selected = scope === 'all' ? Object.values(checks) : [checks[scope]];
  if (!selected[0]) throw new Error(`Escopo V6.4 desconhecido: ${scope}`);
  selected.forEach(check => check());
  console.log(`Release Candidate V6.4 (${scope}): OK`);
}
