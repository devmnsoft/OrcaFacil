import fs from 'node:fs';
const required=['src/OrcaFacil.Web/Pages/Contracts/Dashboard.cshtml','src/OrcaFacil.Application/Contracts/AdvancedContractRules.cs','database/patch_sprint22_contracts_v23.sql'];
const missing=required.filter(x=>!fs.existsSync(x)); if(missing.length) throw new Error(`Contratos avançados ausentes: ${missing.join(', ')}`);
const domain=fs.readFileSync('src/OrcaFacil.Domain/Entities/RecurringContract.cs','utf8'); for(const token of ['PendingApproval','Suspended','Terminated','ContractSlaPolicy','ContractWarrantyTerm','ContractPreventiveSchedule','ContractHealthSnapshot']) if(!domain.includes(token)) throw new Error(`Contrato avançado incompleto: ${token}`);
console.log('Contratos avançados V2.3 validados.');
