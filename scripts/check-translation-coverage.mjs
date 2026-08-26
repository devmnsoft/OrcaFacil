import fs from 'node:fs';
const source = fs.readFileSync('src/OrcaFacil.Application/Localization/LocalizationServices.cs', 'utf8');
const keys = ['Common.Save','Common.Cancel','Common.Delete','Common.Edit','Common.Search','Auth.Login','Auth.Logout','Dashboard.Title','Clients.Title','Quotes.Title','WorkOrders.Title','Finance.Title','Reports.Title','Portal.Title','Public.Home.HeroTitle'];
const resourceFile = 'src/OrcaFacil.Web/Localization/resources.pt-BR.json';
if (!fs.existsSync(resourceFile)) throw new Error('Catálogo pt-BR ausente.');
const catalog = JSON.parse(fs.readFileSync(resourceFile, 'utf8'));
const missing = keys.filter(key => !catalog[key]);
if (!source.includes('SupportedLocales.Default') || missing.length) { console.error(`Chaves ausentes: ${missing.join(', ')}`); process.exit(1); }
console.log(`Cobertura base pt-BR: ${keys.length}/${keys.length}. Idiomas preparados permanecem em rascunho até revisão humana.`);
