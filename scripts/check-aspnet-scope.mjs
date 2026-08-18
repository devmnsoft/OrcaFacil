import { access, readFile } from 'node:fs/promises';

const required = [
  'OrcaFacil.sln',
  'src/OrcaFacil.Web/OrcaFacil.Web.csproj',
  'src/OrcaFacil.Application/OrcaFacil.Application.csproj',
  'src/OrcaFacil.Domain/OrcaFacil.Domain.csproj',
  'src/OrcaFacil.Persistence/OrcaFacil.Persistence.csproj',
  'src/OrcaFacil.Web/Pages/CommercialPipeline/Index.cshtml'
];
await Promise.all(required.map(file => access(file)));

const project = await readFile('src/OrcaFacil.Web/OrcaFacil.Web.csproj', 'utf8');
const forbidden = ['firebase', 'public/', 'server.js'];
const found = forbidden.filter(value => project.toLowerCase().includes(value));
if (found.length) {
  console.error(`O projeto ASP.NET referencia artefatos legados: ${found.join(', ')}`);
  process.exit(1);
}
console.log('Escopo ASP.NET validado: solução, projetos e Pipeline Comercial independentes do projeto legado.');
