import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const root = 'src/OrcaFacil.Web';
const walk = async (dir, extensions) => (await Promise.all((await readdir(dir,{withFileTypes:true})).map(async e => {
  const target=path.join(dir,e.name);
  return e.isDirectory()?walk(target,extensions):(extensions.some(x=>target.endsWith(x))?[target]:[]);
}))).flat();
const pages=await walk(path.join(root,'Pages'),['.cshtml']);
const css=await walk(path.join(root,'wwwroot','css'),['.css']);
const js=await walk(path.join(root,'wwwroot','js'),['.js']);
const load=async files=>Promise.all(files.map(async file=>({file,source:await readFile(file,'utf8')})));
const pageSources=await load(pages), cssSources=await load(css), jsSources=await load(js);
const allCss=cssSources.map(x=>x.source).join('\n');
const failures=[];
const failMatches=(sources,regex,message)=>sources.forEach(({file,source})=>{ regex.lastIndex=0; if(regex.test(source)) failures.push(`${file}: ${message}`); });
const mode=path.basename(process.argv[1]).replace(/^check-|\.mjs$/g,'');

if(mode==='design-system') {
  for(const token of ['--of-primary','--of-focus-ring','--of-transition','--of-z-dialog']) if(!allCss.includes(token)) failures.push(`token ausente: ${token}`);
  for(const component of ['.of-button','.of-card','.of-table','.of-empty-state','.of-timeline','.of-kanban']) if(!allCss.includes(component)) failures.push(`componente ausente: ${component}`);
}
if(mode==='design-consistency') {
  failMatches(pageSources,/\bclass=["'][^"']*(?:^|\s)(?:container-fluid|row|col-(?:sm|md|lg)|btn-(?:primary|secondary))(?:\s|$)/g,'classe visual legada');
}
if(mode==='no-bootstrap-tailwind') failMatches([...pageSources,...cssSources,...jsSources],/(?:data-bs-|new\s+bootstrap\.|@tailwind|tailwindcss|bootstrap(?:\.min)?\.(?:css|js))/gi,'Bootstrap/Tailwind não permitido');
if(mode==='no-fake-buttons') failMatches(pageSources,/<(?:a|button)\b[^>]*(?:href=["'](?:#|javascript:void(?:\(0\))?)["']|data-(?:todo|fake))[^>]*>/gi,'ação sem destino real');
if(mode==='buttons-types') {
  pageSources.forEach(({file,source})=>{ for(const form of source.matchAll(/<form\b[\s\S]*?<\/form>/gi)) if(/<button(?![^>]*\btype=)[^>]*>/i.test(form[0])) failures.push(`${file}: button sem type explícito em form`); });
}
if(mode==='empty-links') failMatches(pageSources,/<a\b[^>]*href\s*=\s*["']\s*(?:#|javascript:void(?:\(0\))?)?\s*["']/gi,'link vazio');
if(mode==='form-labels') {
  pageSources.forEach(({file,source})=>{ for(const input of source.matchAll(/<(?:input|select|textarea)\b[^>]*>/gi)){const tag=input[0]; const before=source.slice(0,input.index); const wrapped=before.lastIndexOf('<label')>before.lastIndexOf('</label>'); if(wrapped||/type=["'](?:hidden|submit|button)["']/i.test(tag)||/aria-label=|asp-for=/i.test(tag)) continue; const id=tag.match(/\bid=["']([^"']+)/i)?.[1]; if(!id||!new RegExp(`<label[^>]*for=["']${id.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')}["']`,'i').test(source)) failures.push(`${file}: controle sem label (${id??'sem id'})`); } });
}
if(mode==='responsive-css') for(const rule of ['@media','prefers-reduced-motion','max-width:767px']) if(!allCss.includes(rule)) failures.push(`regra responsiva ausente: ${rule}`);
if(mode==='accessibility-basic') {
  for(const file of ['_Layout.cshtml','_PublicLayout.cshtml']) { const entry=pageSources.find(x=>x.file.endsWith(file)); if(!entry?.source.includes('of-skip-link')) failures.push(`${file}: skip link ausente`); }
  failMatches(pageSources,/<button\b(?=[^>]*class=["'][^"']*(?:icon|toggle|close))(?!(?:[^>]*aria-label=|[^>]*>\s*[^<\s]))/gi,'botão somente ícone sem nome acessível');
  if(!/:focus-visible/.test(allCss)) failures.push('focus-visible ausente no CSS');
}
if(mode==='icon-consistency') failMatches(pageSources,/<button\b[^>]*>\s*[\u{1F300}-\u{1FAFF}]/gu,'emoji usado como ícone principal');
if(mode==='public-design') for(const item of ['_PublicLayout.cshtml','Index.cshtml','Precos.cshtml','Contato.cshtml']) if(!pageSources.some(x=>x.file.endsWith(item))) failures.push(`página pública ausente: ${item}`);
if(mode==='authenticated-design') for(const item of ['_Layout.cshtml','_AuthenticatedNavigation.cshtml']) if(!pageSources.some(x=>x.file.endsWith(item))) failures.push(`fundação autenticada ausente: ${item}`);
if(mode==='portal-design') for(const fragment of ['/PublicQuotes/','/_PublicQuoteLayout.cshtml']) if(!pageSources.some(x=>x.file.replaceAll('\\','/').includes(fragment))) failures.push(`superfície pública de portal ausente: ${fragment}`);
if(mode==='design-no-raw-pages') pageSources.filter(x=>/^\s*@page/m.test(x.source)).forEach(({file,source})=>{ if(!/Layout\s*=|_ViewStart/.test(source) && !file.endsWith('_ViewStart.cshtml')) {/* inherited layout is valid */} const body=source.replace(/^\s*@(page|model)[^\n]*$/gm,'').trim(); if(body && !/<h1\b|<partial\s+name=/i.test(source)) failures.push(`${file}: página sem título ou partial de cabeçalho identificável`); });
if(mode==='focus-visible' && !/:focus-visible/.test(allCss)) failures.push('focus-visible ausente');

if(failures.length){ console.error(`Falha em ${mode}:\n${failures.join('\n')}`); process.exit(1); }
console.log(`${mode}: OK (${pages.length} Razor Pages, ${css.length} CSS, ${js.length} JS)`);
