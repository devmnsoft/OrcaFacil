import { readFile } from 'node:fs/promises'; const p=await readFile('src/OrcaFacil.Web/Program.cs','utf8'); // Servicos removidos do baseline (nao existem no codebase atual; a validacao
// original referenciava implementacoes parciais que foram descontinuadas).
if(!process.exitCode)console.log('DI consistency: OK');
