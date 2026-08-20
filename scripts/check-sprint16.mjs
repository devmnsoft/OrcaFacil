import fs from 'node:fs';
const checks=[
 ['business units route','src/OrcaFacil.Web/Pages/Settings/BusinessUnits/Index.cshtml'],['teams route','src/OrcaFacil.Web/Pages/Settings/Teams/Index.cshtml'],['approval queue','src/OrcaFacil.Web/Pages/Approvals/Index.cshtml.cs'],['workflow','src/OrcaFacil.Application/Approvals/ApprovalWorkflowService.cs'],['schema','database/patch_release_candidate_schema.sql']];
const missing=checks.filter(([,p])=>!fs.existsSync(p));
const workflow=fs.readFileSync('src/OrcaFacil.Application/Approvals/ApprovalWorkflowService.cs','utf8');
if(!workflow.includes('precisa de aprovação interna')) missing.push(['public proposal guard','workflow']);
if(missing.length){console.error('Sprint 16 incompleta:',missing.map(x=>x[0]).join(', '));process.exit(1)}
console.log('Sprint 16: rotas, workflow e schema validados.');
