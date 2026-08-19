import { readFileSync } from 'node:fs';
const files=['src/OrcaFacil.Web/appsettings.json','src/OrcaFacil.Web/appsettings.Staging.json','src/OrcaFacil.Web/appsettings.Production.json','.env.example'];
for (const file of files) {
  const text=readFileSync(file,'utf8');
  if (/Password=(?!CHANGE_ME)(?![^;\r\n]*\$\{)[^;\r\n]+/i.test(text) || /-----BEGIN (?:RSA |EC )?PRIVATE KEY-----/.test(text)) throw new Error(`Possível segredo real em ${file}`);
}
const production=JSON.parse(readFileSync(files[2],'utf8'));
if (production.ConnectionStrings?.DefaultConnection) throw new Error('Connection string de Production deve ser injetada pelo ambiente.');
if (production.Email?.Password) throw new Error('Senha SMTP de Production deve ser injetada pelo ambiente.');
console.log('Appsettings e .env.example não contêm credenciais reais detectáveis.');
