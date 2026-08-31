import fs from 'node:fs';
const files=['src/OrcaFacil.Application/Automation/AutomationEngine.cs','tests/OrcaFacil.UnitTests/AutomationEngineTests.cs'];
const forbidden=[/NotImplementedException/,/Math\.random/,/DateTime\.Now/,/127\.0\.0\.1:1/,/Database=unavailable/];
const failures=[];
for(const file of files){const text=fs.readFileSync(file,'utf8'); for(const pattern of forbidden) if(pattern.test(text)) failures.push(`${file}: ${pattern}`);}
if(failures.length){console.error(failures.join('\n'));process.exit(1);}
console.log('Automation implementation contains no placeholders or nondeterministic metrics: OK');
