import { readFileSync, existsSync } from 'node:fs';
const required = ['src/OrcaFacil.Web/wwwroot/js/feedback.js','src/OrcaFacil.Web/wwwroot/js/dialogs.js','src/OrcaFacil.Web/wwwroot/js/forms.js','src/OrcaFacil.Web/wwwroot/css/feedback.css','src/OrcaFacil.Web/Pages/Shared/_ToastHost.cshtml','src/OrcaFacil.Web/Pages/Shared/_ConfirmDialog.cshtml'];
for (const file of required) if (!existsSync(file) || readFileSync(file,'utf8').trim().length < 20) throw new Error(`componente V6.1 ausente: ${file}`);
for (const file of ['src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml','src/OrcaFacil.Web/Pages/Documents/New.cshtml','src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml','src/OrcaFacil.Web/Pages/Onboarding/Index.cshtml']) {
 const source=readFileSync(file,'utf8');
 if (!/empty|vazio|nenhum|comece|primeir/i.test(source)) throw new Error(`${file}: empty state ausente`);
}
console.log('design system V6.1: OK');
