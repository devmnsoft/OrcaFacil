import fs from 'node:fs'; const source=fs.readFileSync('src/OrcaFacil.Application/Partners/PartnerSecurityService.cs','utf8');
for(const marker of ['AccountId','PartnerId','AccessRevokedAt']) if(!source.includes(marker)) throw new Error(`Portal sem ${marker}`); console.log('Portal do parceiro: escopo externo validado.');
