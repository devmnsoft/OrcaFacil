import fs from 'node:fs';
const files = ['src/OrcaFacil.Web/Program.cs', 'src/OrcaFacil.Api/Program.cs'];
for (const file of files) {
  const text = fs.readFileSync(file, 'utf8');
  if (!text.includes('DatabaseConnectionStringResolver')) throw new Error(`${file}: resolver centralizado ausente`);
}
const validator = fs.readFileSync('src/OrcaFacil.Persistence/Diagnostics/DatabaseConnectionOptions.cs', 'utf8');
for (const token of ['ORCAFACIL_DATABASE_URL', 'Port == 1', 'Equals("unavailable"'])
  if (!validator.includes(token)) throw new Error(`Validação ausente: ${token}`);
console.log('Database connection configuration: OK');
