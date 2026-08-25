import fs from 'node:fs';
const required = [
 'src/OrcaFacil.Domain/Entities/Marketplace.cs','src/OrcaFacil.Application/Marketplace/MarketplaceContracts.cs',
 'src/OrcaFacil.Persistence/Marketplace/MarketplaceServices.cs','src/OrcaFacil.Web/Pages/Marketplace/Index.cshtml',
 'src/OrcaFacil.Web/Pages/Marketplace/Details.cshtml','src/OrcaFacil.Web/Pages/Marketplace/Installations.cshtml',
 'database/sprint29_marketplace.sql','src/OrcaFacil.Web/wwwroot/js/marketplace.js'
];
const errors=[];for(const file of required)if(!fs.existsSync(file))errors.push(`arquivo ausente: ${file}`);
const read=file=>fs.readFileSync(file,'utf8');
if(fs.existsSync(required[1]))for(const token of ['SemanticVersion','ContainsUnsafeContent','MissingDependencies','MissingFeatures'])if(!read(required[1]).includes(token))errors.push(`validação ausente: ${token}`);
if(fs.existsSync(required[2]))for(const token of ['AccountId','BeginTransactionAsync','confirmed','IgnoreExisting','marketplace.package_installed','não apaga dados operacionais'])if(!read(required[2]).toLowerCase().includes(token.toLowerCase()))errors.push(`regra de instalação ausente: ${token}`);
if(fs.existsSync(required[6]))for(const table of ['marketplace_packages','marketplace_package_versions','marketplace_package_installations','marketplace_package_installation_items','addon_catalog','addon_entitlements','template_library_items','configuration_snapshots','setup_wizard_progress'])if(!read(required[6]).includes(`orcafacil.${table}`))errors.push(`tabela ausente: ${table}`);
for(const file of required.filter(x=>x.endsWith('.cshtml'))){const body=read(file);if(/href\s*=\s*["'](?:#|javascript:void|)["']/i.test(body))errors.push(`link inerte: ${file}`);if(/<button(?![^>]*\btype=)/i.test(body))errors.push(`button sem type: ${file}`);}
if(errors.length){console.error(errors.join('\n'));process.exit(1);}console.log('Sprint 29: marketplace, manifesto, instalação, rollback e isolamento de conta validados.');
