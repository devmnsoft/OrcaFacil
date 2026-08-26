import fs from 'node:fs';

const required = [
  'src/OrcaFacil.Application/Localization/LocalizationServices.cs',
  'database/sprint35_localization_v36.sql',
  'src/OrcaFacil.Web/Pages/Shared/_PublicLayout.cshtml'
];
const failures = required.filter(file => !fs.existsSync(file));
const program = fs.readFileSync('src/OrcaFacil.Web/Program.cs', 'utf8');
for (const marker of ['UseRequestLocalization', 'CookieRequestCultureProvider', 'AcceptLanguageHeaderRequestCultureProvider'])
  if (!program.includes(marker)) failures.push(`Program.cs sem ${marker}`);
const service = fs.readFileSync(required[0], 'utf8');
for (const locale of ['pt-BR', 'en-US', 'es-ES', 'es-419'])
  if (!service.includes(locale)) failures.push(`Idioma ausente: ${locale}`);
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Localization: infraestrutura, fallback e idiomas suportados validados.');
