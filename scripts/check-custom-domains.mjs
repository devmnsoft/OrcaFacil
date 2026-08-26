import { readFileSync, existsSync } from 'node:fs';

const required = [
  'src/OrcaFacil.Domain/Entities/TenantDomains.cs',
  'src/OrcaFacil.Application/Tenants/Domains/TenantDomainServices.cs',
  'src/OrcaFacil.Persistence/Configurations/TenantDomainConfiguration.cs',
  'database/sprint36_custom_domains_v37.sql'
];
for (const file of required) if (!existsSync(file)) throw new Error(`Custom domains: arquivo obrigatório ausente: ${file}`);
const domain = readFileSync(required[0], 'utf8');
const services = readFileSync(required[1], 'utf8');
const sql = readFileSync(required[3], 'utf8');
for (const invariant of ['PendingVerification', 'VerificationTokenHash', 'Activate()', 'NormalizeHost'])
  if (!domain.includes(invariant)) throw new Error(`Custom domains: regra ausente: ${invariant}`);
if (!services.includes('FixedTimeEquals') || !services.includes('SHA256')) throw new Error('Custom domains: token sem comparação/hash seguro.');
if (!sql.includes('WHERE is_deleted=false') || !sql.includes('IF NOT EXISTS')) throw new Error('Custom domains: schema não é idempotente ou permite conflito de host.');
console.log('Custom domains: modelo, segurança e schema verificados.');
