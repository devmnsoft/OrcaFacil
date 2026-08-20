import fs from 'node:fs';
const service=fs.readFileSync('src/OrcaFacil.Infrastructure/Files/LocalFileStorageService.cs','utf8');
const page=fs.readFileSync('src/OrcaFacil.Web/Pages/Files/Index.cshtml.cs','utf8');
for(const required of ['Path.GetFullPath','SHA256','MaximumBytes','wwwroot']) if(required==='wwwroot'?service.includes(required):!service.includes(required)) throw new Error(`file security invariant failed: ${required}`);
for(const required of ['Files.Download','x.AccountId==current.AccountId','StoragePath']) if(!page.includes(required)) throw new Error(`protected download invariant failed: ${required}`);
console.log('File security checks passed.');
