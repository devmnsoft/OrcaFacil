import fs from 'node:fs';
import path from 'node:path';

const roots = ['src/OrcaFacil.Web/Pages', 'src/OrcaFacil.Web/wwwroot/js'];
const extensions = new Set(['.cshtml', '.js']);
const findings = [];
function walk(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const file = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(file);
    else if (extensions.has(path.extname(file))) {
      const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
      lines.forEach((line, i) => {
        if (/(aria-label|placeholder|>)[^<@{}]*[A-Za-zÀ-ÿ]{4}/.test(line) &&
            !/class=|console\.|data-|<script|<style/.test(line)) findings.push(`${file}:${i + 1}`);
      });
    }
  }
}
roots.forEach(walk);
console.log(`Auditoria informativa: ${findings.length} linhas candidatas a recurso.`);
for (const finding of findings.slice(0, 100)) console.log(`- ${finding}`);
console.log('O relatório é não bloqueante; candidatos devem ser revisados para evitar falsos positivos.');
