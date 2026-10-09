import fs from 'node:fs';
import path from 'node:path';

const catalogDir = 'src/OrcaFacil.Web/Localization';
const locales = ['pt-BR', 'en-US', 'es-ES', 'es-419'];
const catalogs = {};

for (const loc of locales) {
  const file = path.join(catalogDir, `resources.${loc}.json`);
  if (!fs.existsSync(file)) {
    console.error(`Catálogo ausente: ${file}`);
    process.exit(1);
  }
  catalogs[loc] = JSON.parse(fs.readFileSync(file, 'utf8'));
}

const baseKeys = Object.keys(catalogs['pt-BR']);
let hasErrors = false;

for (const loc of locales) {
  const current = catalogs[loc];
  const missing = baseKeys.filter(k => current[k] === undefined || !current[k].trim());
  const coverage = ((baseKeys.length - missing.length) / baseKeys.length) * 100;
  console.log(`Cobertura ${loc}: ${baseKeys.length - missing.length}/${baseKeys.length} (${coverage.toFixed(1)}%).`);
  if (missing.length > 0) {
    console.error(`  Chaves não traduzidas em ${loc}: ${missing.join(', ')}`);
    hasErrors = true;
  }
}

if (hasErrors) {
  process.exit(1);
}

console.log('Todos os 4 catálogos de idiomas possuem cobertura completa e consistente.');
