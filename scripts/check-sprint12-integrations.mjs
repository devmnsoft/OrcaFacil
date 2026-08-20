import fs from 'node:fs';
const read = p => fs.readFileSync(p, 'utf8');
const requireText = (p, patterns) => { const s=read(p); for(const x of patterns) if(!s.includes(x)) throw new Error(`${p}: contrato ausente: ${x}`); };
requireText('src/OrcaFacil.Domain/Entities/Integrations.cs',['KeyHash','ProtectedSecret','AccountId','IdempotencyKey']);
requireText('src/OrcaFacil.Application/Integrations/ApiKeyService.cs',['RandomNumberGenerator.GetBytes','SHA256.HashData','FixedTimeEquals']);
requireText('src/OrcaFacil.Web/Pages/Settings/Integrations/Index.cshtml.cs',['IDataProtectionProvider','IntegrationsManage']);
requireText('src/OrcaFacil.Web/Pages/Import/Index.cshtml.cs',['ReadyToImport','DetectDuplicates']);
requireText('src/OrcaFacil.Web/Pages/Exports/Index.cshtml.cs',['AccountId==id','ExportsManage']);
const pages=['src/OrcaFacil.Web/Pages/Settings/Integrations/Index.cshtml','src/OrcaFacil.Web/Pages/Settings/ApiKeys/Index.cshtml','src/OrcaFacil.Web/Pages/Settings/Webhooks/Index.cshtml'];
for(const p of pages){const s=read(p);if(/href=["']#|javascript:void/i.test(s))throw new Error(`${p}: ação falsa`);}
console.log('Sprint 12: integrações, mascaramento, importação e exportação validados.');
