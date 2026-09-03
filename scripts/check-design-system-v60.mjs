import { existsSync, readFileSync } from 'node:fs';
for(const file of ['src/OrcaFacil.Web/wwwroot/js/feedback.js','src/OrcaFacil.Web/wwwroot/js/dialogs.js','src/OrcaFacil.Web/wwwroot/js/forms.js','src/OrcaFacil.Web/wwwroot/css/feedback.css','src/OrcaFacil.Web/Pages/Shared/_ToastHost.cshtml','src/OrcaFacil.Web/Pages/Shared/_ConfirmDialog.cshtml']) if(!existsSync(file)) throw new Error(`${file} ausente`);
const layout=readFileSync('src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml','utf8');
for(const token of ['_ToastHost','_ConfirmDialog','feedback.js','dialogs.js','forms.js']) if(!layout.includes(token)) throw new Error(`Layout sem ${token}`);
console.log('design system v6.0: OK');
