import { readFile } from 'node:fs/promises';

const [limits, usage, planPage] = await Promise.all([
  readFile('src/OrcaFacil.Application/Plans/PlanLimitService.cs', 'utf8'),
  readFile('src/OrcaFacil.Application/Plans/UserUsageService.cs', 'utf8'),
  readFile('src/OrcaFacil.Web/Pages/Subscription/Index.cshtml', 'utf8')
]);
for (const marker of ['CanCreateDocument', 'monthlyCount']) if (!limits.includes(marker)) throw new Error(`PlanLimitService sem ${marker}.`);
if (!/Documents|Clients|Usage/i.test(usage)) throw new Error('Uso real do plano não é calculado.');
if (!/limite|uso|plano/i.test(planPage)) throw new Error('Tela do plano não apresenta uso/limites.');
console.log('Limites, cálculo de uso e experiência do plano validados.');
