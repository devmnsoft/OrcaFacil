import { readFileSync } from 'node:fs';
const files = ['src/OrcaFacil.Web/Pages/Auth/Login.cshtml','src/OrcaFacil.Web/Pages/Onboarding/Index.cshtml','src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml','src/OrcaFacil.Web/Pages/Documents/New.cshtml','src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml','src/OrcaFacil.Web/Pages/Diagnostico.cshtml'];
for (const file of files) { const source=readFileSync(file,'utf8'); if (/<a\b[^>]*href=["']#|javascript:void/i.test(source)) throw new Error(`${file}: link morto`); }
const middleware=readFileSync('src/OrcaFacil.Web/Middleware/DatabaseReadinessMiddleware.cs','utf8').toLowerCase();
for(const route of ['/dashboard','/documents','/commercialroutine','/onboarding']) if(!middleware.includes(route)) throw new Error(`readiness sem ${route}`);
console.log('critical routes: OK');
