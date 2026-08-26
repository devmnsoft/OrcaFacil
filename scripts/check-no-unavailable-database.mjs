import fs from 'node:fs';
const files = ['src/OrcaFacil.Web/Program.cs', 'src/OrcaFacil.Api/Program.cs', 'src/OrcaFacil.Web/appsettings.json', 'src/OrcaFacil.Api/appsettings.json'];
const forbidden = [/Database=unavailable/i, /127\.0\.0\.1[^\n]*Port=1/i];
for (const file of files) for (const pattern of forbidden)
  if (pattern.test(fs.readFileSync(file, 'utf8'))) throw new Error(`${file}: fallback sentinela encontrado`);
console.log('No unavailable database fallback: OK');
