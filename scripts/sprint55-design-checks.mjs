import { readFile, access } from 'node:fs/promises';

const root = 'src/OrcaFacil.Web';
const read = file => readFile(file, 'utf8');
const exists = file => access(file).then(() => true, () => false);
const requireTokens = (source, tokens, label) => tokens.flatMap(token => source.includes(token) ? [] : [`${label}: contrato ausente: ${token}`]);

const cssFiles = ['tokens','base','components','forms','tables','navigation','dashboard','auth','commercial','documents','portals','admin','responsive'];
const criticalPages = ['Pages/Auth/Login.cshtml','Pages/Index.cshtml','Pages/Dashboard/Index.cshtml','Pages/Clients/Index.cshtml','Pages/Clients/Details.cshtml','Pages/Documents/New.cshtml','Pages/Documents/Index.cshtml','Pages/CommercialRoutine/Index.cshtml'];

export async function runSprint55Check(mode) {
  const failures = [];
  const sources = Object.fromEntries(await Promise.all(criticalPages.map(async file => [file, await read(`${root}/${file}`)])));
  const allCritical = Object.values(sources).join('\n');
  const tokens = await read(`${root}/wwwroot/css/tokens.css`);
  const design = await read(`${root}/wwwroot/css/design-system.css`);
  const app = await read(`${root}/wwwroot/js/app.js`);

  if (mode === 'design-system-v56' || mode === 'design-premium') {
    for (const name of cssFiles) if (!await exists(`${root}/wwwroot/css/${name}.css`)) failures.push(`CSS obrigatório ausente: ${name}.css`);
    failures.push(...requireTokens(tokens, ['Design System V5.6','--of-primary','--of-success','--of-danger','--of-shadow-md','--of-radius-xl','--of-transition-fast','--of-icon-md','--of-z-dialog','--of-breakpoint-md'], 'tokens.css'));
    failures.push(...requireTokens(design, ['page-shell','page-hero','page-header','section-header','summary-card','action-card','status-badge','priority-badge','risk-badge','empty-state','loading-state','filter-bar','action-bar','premium-table','responsive-table-card','form-panel','form-grid','input-group','validation-message','timeline','kanban-card','wizard-steps','breadcrumb','quick-actions','command-card','health-card','prefers-reduced-motion'], 'design-system.css'));
  }
  if (mode === 'login-premium') {
    const login = sources['Pages/Auth/Login.cshtml'];
    const auth = await read(`${root}/wwwroot/css/auth.css`);
    failures.push(...requireTokens(login, ['<h1','<form method="post"','AntiForgeryToken','data-loading','data-loading-message','aria-live','/Support/Index','css/auth.css'], 'Login'));
    failures.push(...requireTokens(auth, ['radial-gradient','of-auth-premium-shell','of-auth-security-note','prefers-reduced-motion','@media(max-width:760px)'], 'auth.css'));
  }
  if (mode === 'dashboard-premium') {
    const dashboard = sources['Pages/Dashboard/Index.cshtml'];
    failures.push(...requireTokens(dashboard, ['<h1','dashboard.TotalBudgets','dashboard.BudgetTotal','BestRecommendation','of-quick-actions','of-metric-grid','_EmptyState','Commercial.Attention'], 'Dashboard'));
    if (/Math\.random|Random\s*\(/i.test(dashboard)) failures.push('Dashboard usa valor aleatório.');
  }
  if (mode === 'public-home-design') failures.push(...requireTokens(sources['Pages/Index.cshtml'], ['of-public-hero','<h1','of-home-actions','como-funciona','of-feature-grid','of-final-cta','data-submit-loading'], 'Home pública'));
  if (mode === 'commercial-design') failures.push(...requireTokens(allCritical, ['of-filter-bar','of-action-bar','of-empty-state'], 'Jornadas comerciais'));
  if (mode === 'forms-premium') {
    for (const [file, source] of Object.entries(sources)) {
      for (const match of source.matchAll(/<button\b([^>]*)>/gi)) if (!/\btype\s*=/.test(match[1])) failures.push(`${file}: button sem type`);
      if (/<form\b[^>]*method\s*=\s*["']post["']/i.test(source) && !/(AntiForgeryToken|asp-page|asp-page-handler)/.test(source)) failures.push(`${file}: POST sem antiforgery/tag helper`);
    }
  }
  if (mode === 'tables-premium') {
    const tables = await read(`${root}/wwwroot/css/tables.css`);
    failures.push(...requireTokens(tables + design, ['overflow:auto','premium-table','responsive-table-card','focus-within','@media'], 'Tabelas'));
  }
  if (mode === 'navigation-quality') {
    const navFiles = ['Pages/Shared/_Layout.cshtml','Pages/Shared/_PublicLayout.cshtml','Pages/Shared/Partials/_AuthenticatedNavigation.cshtml'];
    for (const file of navFiles) {
      const source = await read(`${root}/${file}`);
      if (/href\s*=\s*["'](?:#|\s*|javascript:void[^"']*)["']/i.test(source)) failures.push(`${file}: link morto`);
    }
  }
  if (mode === 'portal-design') {
    const layouts = (await read(`${root}/Pages/Shared/_ClientLayout.cshtml`)) + await read(`${root}/wwwroot/css/portals.css`);
    failures.push(...requireTokens(layouts, ['viewport','main','of-client','@media'], 'Portal'));
  }
  if (mode === 'admin-design' || mode === 'system-health-design') {
    const admin = await read(`${root}/Areas/Admin/Pages/Dashboard.cshtml`);
    const adminCss = await read(`${root}/wwwroot/css/admin.css`);
    failures.push(...requireTokens(admin + adminCss, ['<h1','of-admin-primary-metrics','Saúde','Banco','E-mail','of-admin-section'], 'Admin'));
    if (/connection\s*string|password\s*=|stack\s*trace/i.test(admin)) failures.push('Admin expõe diagnóstico sensível.');
  }
  if (mode === 'mobile-layout') {
    const responsive = await read(`${root}/wwwroot/css/responsive.css`) + await read(`${root}/wwwroot/css/mobile.css`) + await read(`${root}/wwwroot/css/navigation.css`) + design;
    for (const width of ['320px','360px','390px','430px','768px','1024px','1440px','1920px']) if (!responsive.includes(width)) failures.push(`viewport sem contrato documentado: ${width}`);
    failures.push(...requireTokens(responsive, ['overflow-x','of-mobile-menu','prefers-reduced-motion'], 'Mobile'));
  }
  if (mode === 'accessibility-basic') {
    failures.push(...requireTokens(app + design + await read(`${root}/wwwroot/css/base.css`) + allCritical, ['safeInit','focus-visible','prefers-reduced-motion','aria-live','aria-label','<h1','<label'], 'Acessibilidade'));
  }
  if (mode === 'feedback-messages') failures.push(...requireTokens(allCritical, ['Ainda não','Criar','Atenção','aria-live'], 'Feedback'));
  if (mode === 'no-technical-id-inputs' && /<(?:input|select)\b[^>]*(?:name|id|asp-for)=["'][^"']*(?:ClientId|UserId|TenantId)[^"']*["']/i.test(allCritical)) failures.push('Tela crítica solicita identificador técnico.');
  if (mode === 'js-safety') {
    failures.push(...requireTokens(app, ['safeInit','if(!toggle||!panel)return','querySelector','pageshow'], 'JavaScript'));
    if (/\bbootstrap\.|new\s+bootstrap|javascript:void/i.test(app)) failures.push('JavaScript depende de Bootstrap ou URL insegura.');
  }
  if (failures.length) { console.error(`Sprint 55 (${mode}):\n- ${failures.join('\n- ')}`); process.exitCode = 1; return; }
  console.log(`Sprint 55 (${mode}): contratos reais validados.`);
}
