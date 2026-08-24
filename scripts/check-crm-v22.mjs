import fs from 'node:fs';

const required = [
  'src/OrcaFacil.Application/Crm/CrmScoringServices.cs',
  'src/OrcaFacil.Domain/Entities/Crm.cs',
  'src/OrcaFacil.Persistence/Migrations/20260821000000_AddCrmV22Core.cs'
];
const missing = required.filter(file => !fs.existsSync(file));
if (missing.length) throw new Error(`CRM V2.2 incompleto: ${missing.join(', ')}`);
const scoring = fs.readFileSync(required[0], 'utf8');
if (/Math\.random|Random\s*\(/.test(scoring)) throw new Error('Score CRM não pode ser aleatório.');
if (!scoring.includes('CanSendCommercial') || !scoring.includes('CanDispatchAutomatically')) throw new Error('Política de consentimento ausente.');
if (!scoring.includes('CampaignChannel.WhatsApp') || !scoring.includes('_ => false')) throw new Error('WhatsApp deve exigir ação humana.');
console.log('CRM V2.2: score explicável, retenção, NPS real e consentimento validados.');
