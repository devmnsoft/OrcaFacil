import fs from 'node:fs';
const read = p => fs.readFileSync(p, 'utf8');
const required = [
 ['src/OrcaFacil.Web/Pages/Analytics/Executive.cshtml','BI Executivo'],
 ['src/OrcaFacil.Web/Pages/Analytics/Forecast.cshtml','Estimativas determinísticas'],
 ['src/OrcaFacil.Web/Pages/Analytics/DataQuality.cshtml','href="@finding.ActionUrl"'],
 ['src/OrcaFacil.Web/Pages/Analytics/AccountHealth.cshtml','Score explicável'],
 ['src/OrcaFacil.Application/Analytics/AnalyticsModels.cs','Sem base comparativa'],
 ['src/OrcaFacil.Domain/Entities/Analytics.cs','TargetValue < 0'],
 ['database/patch_sprint20_analytics_v21.sql','CREATE UNIQUE INDEX IF NOT EXISTS ux_data_quality_finding']
];
const failures=[];
for(const [file,token] of required){if(!fs.existsSync(file)||!read(file).includes(token)) failures.push(`${file}: ausente ${token}`)}
const scope=['src/OrcaFacil.Application/Analytics','src/OrcaFacil.Web/Pages/Analytics'];
for(const dir of scope) for(const file of fs.readdirSync(dir).map(x=>`${dir}/${x}`).filter(x=>fs.statSync(x).isFile())) { const body=read(file); if(/Math\.random|NotImplementedException|href=["']#|javascript:void/i.test(body)) failures.push(`${file}: marcador inseguro/falso`); }
if(failures.length){console.error(failures.join('\n'));process.exit(1)}
console.log('Analytics V2.1: estrutura real, regras determinísticas e links de correção validados.');
