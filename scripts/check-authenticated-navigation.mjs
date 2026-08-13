import { access, readFile } from 'node:fs/promises';
const layout = await readFile('src/OrcaFacil.Web/Pages/Shared/_ClientLayout.cshtml', 'utf8');
const experience = await readFile('src/OrcaFacil.Web/wwwroot/js/experience.js', 'utf8');
const required = ['/Dashboard/Index', '/Documents/Index', '/Clients/Index', '/Auth/Logout'];
const failures = required.filter(route => !layout.includes(route)).map(route => `layout autenticado sem ${route}`);
for (const behavior of ['data-menu-open', "event.key === 'Escape'", "addEventListener('pointerdown'", "closest('a[href]')"]) if (!experience.includes(behavior)) failures.push(`menu móvel sem comportamento: ${behavior}`);
for (const route of required.filter(route => route !== '/Auth/Logout')) try { await access(`src/OrcaFacil.Web/Pages${route}.cshtml`); } catch { failures.push(`página inexistente: ${route}`); }
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Navegação autenticada e menu móvel validados.');
