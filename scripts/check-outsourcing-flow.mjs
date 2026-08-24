import fs from 'node:fs'; const s=fs.readFileSync('src/OrcaFacil.Application/Outsourcing/OutsourcingRules.cs','utf8');
for(const marker of ['ValidateQuoteAcceptance','ValidateAssignmentIsUnique','Accept','Reject','ValidateEvidence','ValidatePaymentRequest']) if(!s.includes(marker)) throw new Error(`Fluxo incompleto: ${marker}`); console.log('Terceirização: regras críticas presentes.');
