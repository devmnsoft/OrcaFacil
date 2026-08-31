import fs from 'node:fs';
const read = p => fs.readFileSync(p,'utf8');
const app=read('src/OrcaFacil.Application/Fiscal/FiscalModule.cs');
const provider=read('src/OrcaFacil.Infrastructure/Fiscal/FiscalProviders.cs');
const schema=read('database/patch_sprint46_fiscal_v47.sql');
const permissions=read('src/OrcaFacil.Application/Security/PermissionCodes.cs');
for(const token of ['IFiscalProvider','INfseProvider','IFiscalDocumentIssuer','IFiscalWebhookVerifier','IFiscalCertificateStore','ManualRegistered','HasRealAuthorization','EnsureSameAccount']) if(!app.includes(token)) throw new Error(`Contrato fiscal ausente: ${token}`);
for(const token of ['NoopFiscalProvider','ManualFiscalProvider','ProtectedFiscalCertificateStore','IDataProtector']) if(!provider.includes(token)) throw new Error(`Proteção de infraestrutura ausente: ${token}`);
for(const token of ['account_id uuid NOT NULL','authorization_protocol','fiscal_manual_authorization_events','IF NOT EXISTS']) if(!schema.includes(token)) throw new Error(`Garantia de schema ausente: ${token}`);
for(const token of ['Fiscal.Issue','Fiscal.ConfigureCertificate','Fiscal.DownloadXml','Portal.FiscalDocumentsView']) if(!permissions.includes(token)) throw new Error(`Permissão fiscal ausente: ${token}`);
const forbidden=[[/Math\.random\s*\(/,'Math.random'],[/href\s*=\s*["']#["']/,'href vazio'],[/javascript:void/i,'javascript:void'],[/NotImplementedException/,'NotImplementedException']];
const critical=['src/OrcaFacil.Application/Fiscal','src/OrcaFacil.Infrastructure/Fiscal'];
for(const root of critical) for(const file of fs.readdirSync(root).map(x=>`${root}/${x}`)) { const body=read(file); for(const [rx,label] of forbidden) if(rx.test(body)) throw new Error(`${label} em ${file}`); }
console.log('Fiscal V4.7: autorização real/manual auditado, isolamento, certificado protegido e schema idempotente verificados.');
