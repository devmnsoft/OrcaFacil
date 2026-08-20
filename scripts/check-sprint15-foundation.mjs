import { readFileSync, existsSync } from 'node:fs';

const [name = 'Sprint 15', ...checks] = process.argv.slice(2);
const failures = [];
for (let index = 0; index < checks.length; index += 2) {
  const file = checks[index];
  const pattern = checks[index + 1];
  if (!existsSync(file)) { failures.push(`${file}: arquivo ausente`); continue; }
  const source = readFileSync(file, 'utf8');
  if (!source.includes(pattern)) failures.push(`${file}: contrato ausente (${pattern})`);
}
if (failures.length) {
  console.error(`❌ ${name}\n- ${failures.join('\n- ')}`);
  process.exitCode = 1;
} else console.log(`✅ ${name}: contratos estruturais validados.`);
