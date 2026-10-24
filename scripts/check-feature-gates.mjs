import fs from 'node:fs';
import path from 'node:path';

// Prova que todo recurso marcado `Ready` no manifesto de prontidão tem gate REAL
// no código (item 7 do roadmap): a constante do recurso é usada fora das definições
// por um handler/serviço que chama CanUseAsync/GetLimitAsync (ou gatilho declarativo
// RequiredFeature com enforcement na base). Também trava que nenhuma entrada
// permaneça `Partial` (débito fechado em 2026-10-19). A marca d'água do PDF é a única
// exceção declarada: gate por tipo de plano (Free) no template, coerente com a matriz
// do catálogo — verificada por tokens explícitos.

const fail = (m) => { throw new Error(`check:feature-gates: ${m}`); };
const read = (p) => fs.readFileSync(p, 'utf8');
const src = 'src';

const manifest = read(`${src}/OrcaFacil.Application/Plans/PlanFeatureReadinessService.cs`);

// 1. Manifesto: 29 entradas Ready, zero Partial.
const ready = [...manifest.matchAll(/Ready\((PlanFeatureCodes\.(\w+)),\s*"([^"]*)",\s*"([^"]*)",\s*"([^"]*)"/g)]
  .map((m) => ({ constRef: m[1], constName: m[2], route: m[3], service: m[4], handler: m[5] }));
if (ready.length !== 29) fail(`esperado 29 entradas Ready no manifesto, obtido ${ready.length}.`);
if (/Partial\(PlanFeatureCodes/.test(manifest)) fail('entrada Partial no manifesto: item 7 do roadmap exige Ready com gate real (ou Missing/Disabled, honestos).');

// 2. Arquivos de definição não contam como gate (só declaram/reclassificam).
const isDefinition = (p) =>
  /PlanCatalogDefinitions|PlanFeatureReadinessService|PlanFeatureCodes\.cs|PlanAccessService|PlanEntitlementService|PlanLimitService|EfPlanAccessDataSource|Migrations/.test(p);

const csFiles = [];
const walk = (dir) => {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(p);
    else if (entry.name.endsWith('.cs')) csFiles.push(p.replaceAll('\\', '/'));
  }
};
walk(src);

const contents = new Map();
for (const file of csFiles) {
  if (isDefinition(file)) continue;
  contents.set(file, read(file));
}

const gateTokens = ['CanUseAsync', 'GetLimitAsync', 'RequiredFeature =>'];

const failures = [];
let watermarks = 0;

for (const entry of ready) {
  // 3. Exceção declarada: marca d'água do PDF (gate por plan type, sem uso da const).
  if (entry.constName === 'PdfWatermark') {
    const limitService = read(`${src}/OrcaFacil.Application/Plans/PlanLimitService.cs`);
    const template = read(`${src}/OrcaFacil.Infrastructure/Pdf/DocumentPdfTemplate.cs`);
    const catalog = read(`${src}/OrcaFacil.Application/Plans/PlanCatalogDefinitions.cs`);
    const enabled = (catalog.match(/\[PlanFeatureCodes\.PdfWatermark\] = Enabled/g) || []).length;
    const disabled = (catalog.match(/\[PlanFeatureCodes\.PdfWatermark\] = Disabled/g) || []).length;
    if (!limitService.includes('PdfHasWatermark') || !limitService.includes('plan == PlanType.Free'))
      failures.push('pdf.watermark: PlanLimitService.PdfHasWatermark sem regra Free.');
    if (!template.includes('Plan == PlanType.Free') || !template.includes('GeneratedWith'))
      failures.push('pdf.watermark: DocumentPdfTemplate não desenha a marca no plano Free.');
    if (enabled !== 1 || disabled !== 3)
      failures.push(`pdf.watermark: matriz inesperada (${enabled} Enabled / ${disabled} Disabled; esperado 1/3).`);
    if (entry.service !== 'QuestPdfDocumentService' || entry.handler !== 'GenerateDocumentPdfAsync')
      failures.push('pdf.watermark: alegação do manifesto mudou (QuestPdfDocumentService.GenerateDocumentPdfAsync).');
    watermarks++;
    continue;
  }

  // 4. Uso real da constante fora das definições, com gate no mesmo arquivo.
  const hits = [...contents.entries()]
    .filter(([file, text]) => text.includes(`PlanFeatureCodes.${entry.constName}`));
  if (hits.length === 0) {
    failures.push(`${entry.constName} (${entry.constRef}): nenhuma aplicação usa a constante (só definições).`);
    continue;
  }
  const gated = hits.filter(([, text]) => gateTokens.some((t) => text.includes(t)));
  if (gated.length === 0) {
    failures.push(
      `${entry.constName} (${entry.constRef}) marcado Ready mas nenhum arquivo que usa a constante chama ` +
      `CanUseAsync/GetLimitAsync/RequiredFeature: ${hits.map(([f]) => path.basename(f)).join(', ')}.`);
  }
}

if (watermarks !== 1) fail(`exceção pdf.watermark esperada 1 vez, obtida ${watermarks}.`);
if (failures.length) fail(`\n  - ${failures.join('\n  - ')}`);

console.log(
  `Feature gates: 29 recursos Ready com gate real verificado no código ` +
  `(28 por CanUseAsync/GetLimitAsync/RequiredFeature + pdf.watermark por plan type no template); 0 Partial.`);
