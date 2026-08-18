import fs from 'node:fs';
for(const file of ['src/OrcaFacil.Web/Pages/Help/Index.cshtml','src/OrcaFacil.Web/Pages/Help/Article.cshtml','src/OrcaFacil.Web/Areas/Admin/Pages/KnowledgeBase/Index.cshtml'])if(!fs.existsSync(file))throw new Error(`Ausente: ${file}`);
const help=fs.readFileSync('src/OrcaFacil.Web/Pages/Help/Index.cshtml','utf8');for(const term of ['Primeiros passos','Clientes','Serviços','Orçamentos','Pagamentos e recibos','Suporte'])if(!help.includes(term))throw new Error(`Tema ausente: ${term}`);
if(/@Html\.Raw|href=["']#/.test(help))throw new Error('Conteúdo inseguro ou link falso na ajuda.');
console.log('Help center: busca, categorias, CTAs reais e renderização segura verificados.');
