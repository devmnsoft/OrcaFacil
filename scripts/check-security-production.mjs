import { readFileSync } from 'node:fs';
const program = readFileSync('src/OrcaFacil.Web/Program.cs','utf8');
for (const marker of ['Cookie.HttpOnly = true','CookieSecurePolicy.Always','SameSiteMode.Strict','UseHsts','X-Content-Type-Options','Content-Security-Policy','UseAuthorization']) if (!program.includes(marker)) throw new Error(`Proteção ausente: ${marker}`);
if (/EnableSensitiveDataLogging\(true\)/.test(program)) throw new Error('Sensitive data logging não pode ser habilitado.');
const health = program.slice(program.indexOf('static Task WritePublicHealth'), program.indexOf('app.MapPost'));
if (/connectionString|exceptionMessage|password|pepper|token/i.test(health)) throw new Error('/health pode expor detalhes internos.');
console.log('Segurança de produção e contrato público sanitizado do health check OK.');
