import { readdir, readFile, access } from 'node:fs/promises';
import { extname, relative, join } from 'node:path';

const root = process.cwd();
const allowed = new Set(['.cs', '.cshtml', '.js', '.mjs', '.json', '.sql', '.ps1']);
async function files(directory) {
  const result = [];
  try {
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      if (['bin', 'obj', 'node_modules', '.git'].includes(entry.name)) continue;
      const path = join(directory, entry.name);
      if (entry.isDirectory()) result.push(...await files(path));
      else if (allowed.has(extname(entry.name))) result.push(path);
    }
  } catch (error) { if (error.code !== 'ENOENT') throw error; }
  return result;
}
const rules = {
  source: [
    ['NotImplementedException', /throw\s+new\s+NotImplementedException/, 'implementação ausente'],
    ['empty catch', /catch\s*(?:\([^)]*\))?\s*\{\s*\}/s, 'catch vazio'],
    ['invalid database fallback', /Database=unavailable|127\.0\.0\.1:1/i, 'fallback inválido'],
  ],
  conversion: [['Guid.Parse', /Guid\.Parse\s*\(/, 'Guid.Parse sem validação']],
  razor: [
    ['empty link', /href\s*=\s*["'](?:#|javascript:void[^"']*)["']/i, 'link vazio'],
    ['button type', /<button(?![^>]*\btype\s*=)[^>]*>/i, 'button sem type'],
  ],
  js: [['random metric', /Math\.random\s*\(/, 'Math.random não determinístico'], ['bootstrap dependency', /\b(?:new\s+)?bootstrap\./, 'dependência Bootstrap']],
};
export async function runAudit(profile = 'source') {
  const scanFiles = await files(join(root, 'src'));
  const selected = rules[profile] ?? rules.source;
  const violations = [];
  for (const file of scanFiles) {
    if (file.endsWith('FunctionalQualityServices.cs')) continue;
    const content = await readFile(file, 'utf8');
    for (const [, pattern, message] of selected) if (pattern.test(content)) violations.push(`${relative(root, file)}: ${message}`);
  }
  if (violations.length) { console.error(violations.join('\n')); process.exitCode = 1; return; }
  console.log(`${profile} consistency: OK (${scanFiles.length} ASP.NET files audited)`);
}
export async function requirePaths(paths, label) {
  const missing=[]; for (const path of paths) try { await access(join(root,path)); } catch { missing.push(path); }
  if(missing.length){console.error(`${label}: ausente: ${missing.join(', ')}`);process.exitCode=1;} else console.log(`${label}: OK`);
}
