import fs from 'node:fs';
import path from 'node:path';

const roots = ['src/OrcaFacil.Application/Bi', 'src/OrcaFacil.Web/Pages/BI', 'src/OrcaFacil.Web/wwwroot/js'];
const extensions = new Set(['.cs', '.cshtml', '.js']);
const forbidden = [/Math\.random\s*\(/, /new\s+Random\s*\(/, /href\s*=\s*["']#["']/i, /javascript:void/i, /NotImplementedException/];
const files = [];
function visit(directory) {
  if (!fs.existsSync(directory)) return;
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const item = path.join(directory, entry.name);
    if (entry.isDirectory()) visit(item); else if (extensions.has(path.extname(item))) files.push(item);
  }
}
roots.forEach(visit);
const violations = files.flatMap(file => {
  const content = fs.readFileSync(file, 'utf8');
  return forbidden.filter(rule => rule.test(content)).map(rule => `${file}: ${rule}`);
});
if (violations.length) { console.error(violations.join('\n')); process.exit(1); }
console.log(`BI safety check passed (${files.length} files inspected).`);

