import fs from 'node:fs/promises';
const bump = process.argv[2];
if (!['patch','minor','major'].includes(bump)) { console.error('Uso: node scripts/release.mjs patch|minor|major'); process.exit(1); }
const pkg = JSON.parse(await fs.readFile('package.json','utf8'));
const v = pkg.version.split('.').map(Number);
if (bump === 'major') { v[0]++; v[1]=0; v[2]=0; } else if (bump === 'minor') { v[1]++; v[2]=0; } else v[2]++;
const next = v.join('.');
pkg.version = next;
await fs.writeFile('package.json', JSON.stringify(pkg,null,2)+'\n');
let cfg = await fs.readFile('public/js/core/config.js','utf8');
cfg = cfg.replace(/version: '[^']+'/, `version: '${next}'`);
await fs.writeFile('public/js/core/config.js', cfg);
const today = new Date().toISOString().slice(0,10);
let changelog = await fs.readFile('CHANGELOG.md','utf8').catch(() => '# Changelog\n');
changelog = changelog.replace('# Changelog\n', `# Changelog\n\n## [${next}] - ${today}\n\n### Alterado\n- Preparação de release ${next}.\n`);
await fs.writeFile('CHANGELOG.md', changelog);
console.log(`Versão atualizada para ${next}`);
console.log(`Commit sugerido: chore(release): ${next}`);
