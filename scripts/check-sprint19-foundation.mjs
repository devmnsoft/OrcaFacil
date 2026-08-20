import fs from 'node:fs';
const read = (file) => { if (!fs.existsSync(file)) throw new Error(`Arquivo obrigatório ausente: ${file}`); return fs.readFileSync(file,'utf8'); };
const requireText = (file, values) => { const source=read(file); for(const value of values) if(!source.includes(value)) throw new Error(`${file}: contrato ausente: ${value}`); };
const mode=process.argv[2];
const checks={
 search(){requireText('src/OrcaFacil.Web/Services/GlobalSearchService.cs',['AccountId == accountId','PermissionCodes.SearchGlobal','AsNoTracking()','FileAssetVisibility.Private']);requireText('src/OrcaFacil.Web/Pages/Search/Index.cshtml',['Nenhum resultado encontrado','/Search']);},
 command(){requireText('src/OrcaFacil.Web/Pages/CommandCenter/Index.cshtml.cs',['HasPermissionAsync','CommandCenterUse']);requireText('src/OrcaFacil.Web/Pages/CommandCenter/Index.cshtml',['data-command-item']);},
 assistant(){requireText('src/OrcaFacil.Web/Services/InternalAssistantService.cs',['Resposta baseada nas regras do OrçaFácil','AccountId == accountId','não vou inventar','AsNoTracking()']);const source=read('src/OrcaFacil.Web/Services/InternalAssistantService.cs');if(/HttpClient|ChatGPT|OpenAI|ExecuteSqlRaw|SaveChanges/.test(source))throw new Error('Assistente contém provedor ou mutação não autorizada.');},
 knowledge(){requireText('src/OrcaFacil.Web/Pages/Help/Index.cshtml.cs',['IsPublished','IsDeleted','AsNoTracking']);},
 tours(){requireText('src/OrcaFacil.Application/Security/PermissionCodes.cs',['GuidedTours.View','GuidedTours.Manage']);},
 onboarding(){requireText('src/OrcaFacil.Application/Security/PermissionCodes.cs',['Onboarding.Manage']);requireText('src/OrcaFacil.Web/Pages/Onboarding/Index.cshtml.cs',['OnGetAsync']);},
 productivity(){requireText('src/OrcaFacil.Web/Pages/Productivity/Index.cshtml.cs',['AccountId']);requireText('src/OrcaFacil.Web/Services/NavigationMapService.cs',['/Productivity/Index']);},
 nofake(){const source=read('src/OrcaFacil.Web/Pages/Assistant/Index.cshtml');if(/\bIA\b|inteligência artificial/i.test(source))throw new Error('Assistente promete IA sem provedor configurado.');requireText('src/OrcaFacil.Web/Pages/Assistant/Index.cshtml',['Nenhuma ação é executada automaticamente']);}
};
if(!checks[mode])throw new Error(`Check Sprint 19 desconhecido: ${mode}`);checks[mode]();console.log(`Sprint 19 (${mode}): OK`);
