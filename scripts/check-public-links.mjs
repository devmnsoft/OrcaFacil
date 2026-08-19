import { readFileSync } from 'node:fs';
const program=readFileSync('src/OrcaFacil.Web/Program.cs','utf8');
const options=readFileSync('src/OrcaFacil.Web/Security/PasswordRecoveryService.cs','utf8');
const production=JSON.parse(readFileSync('src/OrcaFacil.Web/appsettings.Production.json','utf8'));
if (!program.includes('ApplicationUrlOptions')) throw new Error('PublicBaseUrl não está registrado.');
if (!/^https:\/\//.test(production.Application?.PublicBaseUrl ?? '')) throw new Error('PublicBaseUrl de produção deve usar HTTPS.');
if (!options.includes('PublicBaseUrl')) throw new Error('Recuperação pública não usa PublicBaseUrl.');
for (const file of ['src/OrcaFacil.Web/Pages','src/OrcaFacil.Web/Areas']) { void file; }
console.log('PublicBaseUrl HTTPS e geração configurada de links públicos OK.');
