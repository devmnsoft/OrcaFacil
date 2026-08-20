import { readFileSync, existsSync } from 'node:fs';
const checks = [
 ['recommendation route', 'src/OrcaFacil.Web/Pages/Recommendations/Index.cshtml'],
 ['recommendation service', 'src/OrcaFacil.Web/Services/RecommendationService.cs'],
 ['scoring rules', 'src/OrcaFacil.Application/Scoring/CommercialScoring.cs'],
 ['executive reporting', 'src/OrcaFacil.Web/Services/IntelligenceReportService.cs'],
 ['productivity storage', 'src/OrcaFacil.Domain/Entities/ProductivityIntelligence.cs']
];
const missing=checks.filter(([,p])=>!existsSync(p));
const score=readFileSync(checks[2][1],'utf8');
const automation=readFileSync(checks[4][1],'utf8');
if (/Math\.random|\bRandom\s*\(/.test(score)) throw new Error('Score aleatório não é permitido.');
if (/whatsapp|send.*email|charge.*automatic/i.test(automation)) throw new Error('Automação externa/financeira automática detectada.');
if (missing.length) throw new Error(`Arquivos ausentes: ${missing.map(x=>x[0]).join(', ')}`);
console.log('Sprint 11: contratos de recomendações, scoring, automação segura e produtividade validados.');
