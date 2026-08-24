import fs from 'node:fs';
const required=['src/OrcaFacil.Domain/Entities/Partners.cs','src/OrcaFacil.Application/Partners/PartnerSecurityService.cs','src/OrcaFacil.Persistence/Configurations/PartnerConfiguration.cs'];
for(const file of required) if(!fs.existsSync(file)) throw new Error(`Ausente: ${file}`);
console.log('Parceiros V2.7: domínio, isolamento e persistência presentes.');
