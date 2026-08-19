import { readFile } from 'node:fs/promises';

const [audit, entity, page] = await Promise.all([
  readFile('src/OrcaFacil.Persistence/AuditService.cs', 'utf8'),
  readFile('src/OrcaFacil.Domain/Entities/AuditLog.cs', 'utf8'),
  readFile('src/OrcaFacil.Web/Areas/Admin/Pages/Audit/Index.cshtml.cs', 'utf8')
]);
const forbidden = [/PasswordHash\s*=/, /ConnectionString\s*=/, /SmtpPassword\s*=/, /Token\s*=/];
for (const pattern of forbidden) if (pattern.test(audit)) throw new Error(`Auditoria pode persistir segredo: ${pattern}.`);
for (const field of ['AccountId','UserId','Action','EntityType']) if (!entity.includes(field)) throw new Error(`AuditLog sem ${field}.`);
if (!page.includes('Take(200)')) throw new Error('Consulta de auditoria não possui limite operacional.');
console.log('Contrato, sanitização e consulta paginada de auditoria validados.');
