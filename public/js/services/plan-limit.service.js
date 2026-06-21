export const DEFAULT_PLAN_RULES = {
  free: { name:'Free', priceMonthly:0, maxDocumentsPerMonth:20, maxPdfPerMonth:20, watermark:true, historyLimit:20, allowLogo:true, allowPublicApproval:false, allowBackupExport:true },
  pro: { name:'Pro', priceMonthly:19.90, priceYearly:199.00, maxDocumentsPerMonth:null, maxPdfPerMonth:null, watermark:false, historyLimit:null, allowLogo:true, allowPublicApproval:true, allowBackupExport:true }
};
const LIMIT_MESSAGE = 'Você atingiu o limite mensal do plano Free. Para continuar gerando documentos sem limite e remover a marca dos PDFs, ative o plano Pro.';
function planOf(user){ return user?.plan === 'pro' ? 'pro' : 'free'; }
function rules(plan){ return DEFAULT_PLAN_RULES[plan === 'pro' ? 'pro' : 'free']; }
function usageNumber(usage, key){ return Number(usage?.[key] || 0); }
function result(ok, message=''){ return { ok, allowed:ok, message }; }
export function getPlanRules(plan){ return rules(plan); }
export function canCreateDocument(user, usage={}){ const r=rules(planOf(user)); return r.maxDocumentsPerMonth == null || usageNumber(usage,'documentsCreated') < r.maxDocumentsPerMonth ? result(true) : result(false, LIMIT_MESSAGE); }
export function canGeneratePdf(user, usage={}){ const r=rules(planOf(user)); return r.maxPdfPerMonth == null || usageNumber(usage,'pdfGenerated') < r.maxPdfPerMonth ? result(true) : result(false, LIMIT_MESSAGE); }
export function canUsePublicApproval(user){ const r=rules(planOf(user)); return r.allowPublicApproval ? result(true) : result(false,'Link de aprovação pública é um recurso do plano Pro.'); }
export function canExportBackup(user){ const r=rules(planOf(user)); return r.allowBackupExport ? result(true) : result(false,'Exportação não disponível para o seu plano.'); }
export function getUsageMessage(user, usage={}){ const r=rules(planOf(user)); if(planOf(user)==='pro') return 'Plano Pro ativo: uso sem limites práticos no MVP.'; return `Plano Free: ${usageNumber(usage,'documentsCreated')}/${r.maxDocumentsPerMonth} documentos e ${usageNumber(usage,'pdfGenerated')}/${r.maxPdfPerMonth} PDFs no mês.`; }
export const PlanLimitService = { getPlanRules, canCreateDocument, canGeneratePdf, canUsePublicApproval, canExportBackup, getUsageMessage, DEFAULT_PLAN_RULES };
