import fs from 'node:fs';
import path from 'node:path';

const required = [
  'src/OrcaFacil.Application/Localization/LocalizationServices.cs',
  'src/OrcaFacil.Application/Localization/TextLocalizer.cs',
  'database/sprint35_localization_v36.sql',
  'src/OrcaFacil.Web/Pages/Shared/_PublicLayout.cshtml',
  'src/OrcaFacil.Web/Pages/Shared/_ClientLayout.cshtml'
];

const failures = [];

for (const file of required) {
  if (!fs.existsSync(file)) {
    failures.push(`Arquivo obrigatório ausente: ${file}`);
  }
}

const program = fs.readFileSync('src/OrcaFacil.Web/Program.cs', 'utf8');
for (const marker of ['UseRequestLocalization', 'CookieRequestCultureProvider', 'AcceptLanguageHeaderRequestCultureProvider']) {
  if (!program.includes(marker)) {
    failures.push(`Program.cs sem ${marker}`);
  }
}

const locales = ['pt-BR', 'en-US', 'es-ES', 'es-419'];
const catalogs = {};
const catalogDir = 'src/OrcaFacil.Web/Localization';

for (const loc of locales) {
  const filePath = path.join(catalogDir, `resources.${loc}.json`);
  if (!fs.existsSync(filePath)) {
    failures.push(`Catálogo do idioma ${loc} ausente: ${filePath}`);
    continue;
  }
  try {
    catalogs[loc] = JSON.parse(fs.readFileSync(filePath, 'utf8'));
  } catch (err) {
    failures.push(`Erro ao parsear JSON do catálogo ${loc}: ${err.message}`);
  }
}

if (!catalogs['pt-BR']) {
  console.error('Catálogo base pt-BR indisponível para verificação.');
  process.exit(1);
}

const baseKeys = Object.keys(catalogs['pt-BR']);
console.log(`Catálogo base pt-BR: ${baseKeys.length} chaves registradas.`);

const placeholderRegex = /\{(\d+)\}/g;

for (const loc of locales) {
  if (loc === 'pt-BR' || !catalogs[loc]) continue;
  const currentCatalog = catalogs[loc];
  const missingKeys = [];
  const emptyKeys = [];
  const placeholderMismatches = [];

  for (const key of baseKeys) {
    const baseValue = catalogs['pt-BR'][key];
    const targetValue = currentCatalog[key];

    if (targetValue === undefined) {
      missingKeys.push(key);
      continue;
    }

    if (typeof targetValue !== 'string' || !targetValue.trim()) {
      emptyKeys.push(key);
      continue;
    }

    // Validação estrita de placeholders {0}, {1}, etc.
    const baseMatches = Array.from(baseValue.matchAll(placeholderRegex), m => m[1]).sort();
    const targetMatches = Array.from(targetValue.matchAll(placeholderRegex), m => m[1]).sort();
    if (JSON.stringify(baseMatches) !== JSON.stringify(targetMatches)) {
      placeholderMismatches.push(`${key} (base: [${baseMatches.join(',')}], alvo: [${targetMatches.join(',')}])`);
    }
  }

  const coverage = ((baseKeys.length - missingKeys.length) / baseKeys.length) * 100;
  console.log(`Idioma ${loc}: ${baseKeys.length - missingKeys.length}/${baseKeys.length} chaves (${coverage.toFixed(1)}% cobertura real).`);

  if (missingKeys.length > 0) {
    failures.push(`[${loc}] Chaves ausentes: ${missingKeys.slice(0, 10).join(', ')}${missingKeys.length > 10 ? '...' : ''}`);
  }
  if (emptyKeys.length > 0) {
    failures.push(`[${loc}] Chaves vazias: ${emptyKeys.join(', ')}`);
  }
  if (placeholderMismatches.length > 0) {
    failures.push(`[${loc}] Placeholders divergentes: ${placeholderMismatches.join(', ')}`);
  }
}

if (failures.length > 0) {
  console.error('Falhas de validação de localização encontradas:');
  console.error(failures.join('\n'));
  process.exit(1);
}

console.log('✓ Verificação de localização concluída com sucesso: todos os 4 idiomas sincronizados, chaves e placeholders válidos.');
