#!/usr/bin/env node
import { readFileSync } from 'node:fs';
import { extname, relative } from 'node:path';
import { execFileSync } from 'node:child_process';

const root = process.cwd();
const allowedExtensions = new Set(['.cshtml', '.html', '.css', '.js']);
const files = execFileSync('git', ['ls-files'], { cwd: root, encoding: 'utf8' })
  .split('\n')
  .filter(Boolean)
  .filter((file) => allowedExtensions.has(extname(file)) && !file.includes('node_modules/'));

const riskyPatterns = [
  {
    name: 'Texto branco em superfície clara',
    regex: /class=\"[^\"]*(?:bg-(?:white|light)|of-card|surface-light)[^\"]*(?:text-white)[^\"]*\"|\.(?:of-card|surface-light)[^{]*\{(?=[^}]*background(?:-color)?\s*:\s*(?:#fff|#ffffff|white))(?=[^}]*color\s*:\s*(?:#fff|#ffffff|white))/i,
    hint: 'Use texto escuro em cards, fundos brancos ou seções claras.'
  },
  {
    name: 'Texto muted em superfície escura',
    regex: /class=\"[^\"]*(?:bg-(?:dark|primary)|of-sidebar|hero|surface-dark)[^\"]*text-muted[^\"]*\"/i,
    hint: 'Defina cor secundária clara explícita em fundos escuros.'
  },
  {
    name: 'Combinação azul escuro sobre azul escuro',
    regex: /(background(?:-color)?\s*:\s*#(?:0b1f3a|102a43|0f172a|111827)[^}]{0,220}color\s*:\s*#(?:0b1f3a|102a43|0f172a|111827))/i,
    hint: 'Evite título/texto azul escuro sobre fundo azul escuro.'
  },
  {
    name: 'Card claro com cor branca explícita',
    regex: /\.(?:card|of-card|.*surface.*)\s*\{[^}]*background(?:-color)?\s*:\s*(?:#fff|#ffffff|white)[^}]*color\s*:\s*(?:#fff|#ffffff|white)/i,
    hint: 'Cards claros precisam declarar texto escuro.'
  }
];

const findings = [];
for (const file of files) {
  const content = readFileSync(file, 'utf8');
  const lines = content.split(/\r?\n/);
  for (const pattern of riskyPatterns) {
    const match = pattern.regex.exec(content);
    if (!match) continue;
    const line = content.slice(0, match.index).split(/\r?\n/).length;
    findings.push({ file: relative(root, file), line, ...pattern });
  }
}

if (findings.length > 0) {
  console.error('Falha na auditoria automática de contraste. Revise os padrões abaixo:');
  for (const finding of findings) {
    console.error(`- ${finding.file}:${finding.line} — ${finding.name}. ${finding.hint}`);
  }
  process.exit(1);
}

console.log(`Auditoria automática de contraste concluída: ${files.length} arquivos verificados, nenhum padrão bloqueador encontrado.`);
