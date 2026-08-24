import fs from 'node:fs';
const files=['src/OrcaFacil.Application/Contracts/AdvancedContractRules.cs','src/OrcaFacil.Domain/Entities/RecurringContract.cs','database/patch_sprint22_contracts_v23.sql'];
const source=files.map(x=>fs.readFileSync(x,'utf8')).join('\n');
if(/Math\.random|NotImplementedException|href=["']#|javascript:void/i.test(source)) throw new Error('Implementação contratual contém marcador inseguro ou falso.');
if(!source.includes('IdempotencyKey')||!source.includes('AccountId')) throw new Error('Escopo da conta ou idempotência ausente.');
console.log('Check Sprint 22: contract-sla aprovado.');
