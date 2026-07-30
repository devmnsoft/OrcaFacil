import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const root = process.cwd();
const pageRoot = path.join(root, 'src/OrcaFacil.Web/Pages');
const adminRoot = path.join(root, 'src/OrcaFacil.Web/Areas/Admin/Pages');
const exceptionFile = path.join(root, 'scripts/authenticated-ui-legacy-exceptions.json');
const publicLayouts = /_Public(?:Quote)?Layout\.cshtml|_AuthLayout\.cshtml/;
const publicDirectories = new Set(['Auth', 'PublicQuotes']);
const publicPages = new Set([
  'Cadastro.cshtml', 'ComoFunciona.cshtml', 'CondicoesComerciais.cshtml', 'Cookies.cshtml',
  'Error.cshtml', 'Index.cshtml', 'Login.cshtml', 'Precos.cshtml', 'Privacidade.cshtml',
  'Termos.cshtml', 'Support/Index.cshtml'
]);

const rules = [
  ['Bootstrap icon', /\bbi\s+bi-[\w-]+\b/],
  ['Bootstrap data API', /\bdata-bs-[\w-]+/],
  ['Bootstrap JavaScript', /\bbootstrap\s*\./],
  ['Bootstrap CDN', /(?:cdn\.jsdelivr\.net|cdnjs\.cloudflare\.com)[^"']*bootstrap/i],
  ['jQuery', /(?:\bjQuery\b|\$\s*\()/],
  ['legacy modal partial', /_(?:ConfirmModal|FeatureDemoModal)\b/]
];
const bootstrapClass = /^(?:btn(?:-[\w-]+)?|row|col(?:-[\w-]+)+|table(?:-[\w-]+)?|modal(?:-[\w-]+)?|badge|alert(?:-[\w-]+)?|form-control|form-select|pagination|text-bg-[\w-]+|d-(?:flex|grid)|m[bt]-\d+|p[xy]-\d+)$/;

async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = await Promise.all(entries.map(async entry => {
    const target = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(target) : [target];
  }));
  return files.flat();
}

function relative(file) { return path.relative(root, file).split(path.sep).join('/'); }

async function isAuthenticatedClientPage(file) {
  const rel = relative(file).replace('src/OrcaFacil.Web/Pages/', '');
  if (publicPages.has(rel) || publicDirectories.has(rel.split('/')[0])) return false;
  const content = await readFile(file, 'utf8');
  return !publicLayouts.test(content);
}

const candidates = [
  ...(await walk(pageRoot)).filter(file => file.endsWith('.cshtml')),
  ...(await walk(adminRoot)).filter(file => file.endsWith('.cshtml'))
];
const files = [];
for (const file of candidates) {
  if (file.startsWith(adminRoot) || await isAuthenticatedClientPage(file)) files.push(file);
}

const violations = [];
for (const file of files) {
  const lines = (await readFile(file, 'utf8')).split(/\r?\n/);
  lines.forEach((line, index) => {
    const classAttributes = [...line.matchAll(/\bclass\s*=\s*["']([^"']*)["']/g)];
    if (classAttributes.some(match => match[1].split(/\s+/).some(token => bootstrapClass.test(token)))) {
      violations.push({ file: relative(file), line: index + 1, kind: 'Bootstrap class', source: line.trim() });
    }
    for (const [kind, pattern] of rules) {
      if (pattern.test(line)) violations.push({ file: relative(file), line: index + 1, kind, source: line.trim() });
    }
  });
}

let exceptions = [];
try { exceptions = JSON.parse(await readFile(exceptionFile, 'utf8')); }
catch (error) {
  if (error.code !== 'ENOENT') throw error;
}

const required = ['file', 'reason', 'owner', 'deadline', 'issue'];
const invalid = exceptions.filter(item => required.some(field => !item[field]) || Number.isNaN(Date.parse(item.deadline)));
if (invalid.length) {
  console.error(`Invalid authenticated UI exception entries: ${invalid.length}. Required: ${required.join(', ')}.`);
  process.exit(1);
}

const expired = exceptions.filter(item => Date.parse(`${item.deadline}T23:59:59Z`) < Date.now());
if (expired.length) {
  console.error(`Expired authenticated UI exceptions:\n${expired.map(item => `- ${item.file} (${item.deadline})`).join('\n')}`);
  process.exit(1);
}

const allowed = new Set(exceptions.map(item => item.file));
const blocked = violations.filter(item => !allowed.has(item.file));
if (blocked.length) {
  console.error('Authenticated UI legacy dependencies found:');
  for (const item of blocked) console.error(`- ${item.file}:${item.line} [${item.kind}] ${item.source}`);
  console.error('\nRemove the dependency or add a complete, time-bound exception.');
  process.exit(1);
}

const exceptedFiles = new Set(violations.filter(item => allowed.has(item.file)).map(item => item.file));
console.log(`Authenticated UI check passed: ${files.length} Razor files scanned, ${violations.length} legacy references covered by ${exceptedFiles.size} temporary file exceptions.`);
