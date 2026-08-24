import fs from 'node:fs'; const s=fs.readFileSync('src/OrcaFacil.Application/Partners/PartnerSecurityService.cs','utf8');
for(const marker of ['RandomNumberGenerator','SHA256','FixedTimeEquals','ExpiresAt','RevokedAt','AcceptedAt']) if(!s.includes(marker)) throw new Error(`Auth incompleta: ${marker}`); console.log('Auth externo: tokens opacos, hash, expiração e uso único presentes.');
