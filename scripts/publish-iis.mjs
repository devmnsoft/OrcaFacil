import fs from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const root = process.cwd();
const publicDir = path.join(root, 'public');
const distDir = path.join(root, 'dist');
const publishDir = path.join(root, 'publish');
const pkg = JSON.parse(await fs.readFile(path.join(root, 'package.json'), 'utf8'));

const optionalRootFiles = ['aprovar.html', 'diagnostico.html', 'instalacao.html', 'termos.html', 'privacidade.html', 'pagamento-sucesso.html', 'pagamento-pendente.html', 'pagamento-falha.html'];
const copyDirs = ['css', 'js', 'assets', 'vendor'];
const forbiddenDistEntries = ['.env', path.join('functions', '.env'), 'node_modules', 'scripts', 'tests', '.git'];
const sensitiveTerms = [
  'serviceAccount',
  'private_key',
  'TELEGRAM_BOT_TOKEN',
  'MERCADO_PAGO_ACCESS_TOKEN',
  'OPENAI_API_KEY'
];

function log(message) {
  console.log(`[publish-iis] ${message}`);
}

async function exists(filePath) {
  try {
    await fs.access(filePath);
    return true;
  } catch {
    return false;
  }
}

async function ensureDir(dirPath) {
  await fs.mkdir(dirPath, { recursive: true });
}

async function walk(dir) {
  const files = [];
  if (!await exists(dir)) return files;
  for (const entry of await fs.readdir(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) files.push(...await walk(fullPath));
    else files.push(fullPath);
  }
  return files;
}

function isBlockedName(name) {
  const lower = name.toLowerCase();
  return lower === '.env'
    || lower === '.env.local'
    || lower === '.env.production'
    || lower.includes('serviceaccount')
    || lower.includes('private_key')
    || lower.endsWith('.pem')
    || lower.endsWith('.key');
}

async function copyDirSafe(from, to) {
  if (!await exists(from)) return;
  await ensureDir(to);
  for (const entry of await fs.readdir(from, { withFileTypes: true })) {
    if (entry.name.startsWith('.') || isBlockedName(entry.name) || entry.name.endsWith('.map')) continue;
    const source = path.join(from, entry.name);
    const target = path.join(to, entry.name);
    if (entry.isDirectory()) {
      if (['node_modules', 'scripts', 'tests', '.git', '.github', 'functions'].includes(entry.name)) continue;
      await copyDirSafe(source, target);
    } else {
      await ensureDir(path.dirname(target));
      await fs.copyFile(source, target);
    }
  }
}

function minifyJs(code) {
  return code
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/(^|[^:])\/\/.*$/gm, '$1')
    .replace(/\s+/g, ' ')
    .replace(/\s*([{}()[\];,:?+*/%<>=|&!-])\s*/g, '$1')
    .trim() + '\n';
}

function minifyCss(code) {
  return code
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/\s+/g, ' ')
    .replace(/\s*([{}:;,>])\s*/g, '$1')
    .trim() + '\n';
}

async function minifyKnownAssets() {
  for (const file of await walk(path.join(distDir, 'css'))) {
    if (file.endsWith('.css')) await fs.writeFile(file, minifyCss(await fs.readFile(file, 'utf8')));
  }
  for (const file of await walk(path.join(distDir, 'js'))) {
    if (file.endsWith('.js') || file.endsWith('.mjs')) await fs.writeFile(file, minifyJs(await fs.readFile(file, 'utf8')));
  }
}

const webConfig = `<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>

    <defaultDocument enabled="true">
      <files>
        <clear />
        <add value="index.html" />
      </files>
    </defaultDocument>

    <staticContent>
      <remove fileExtension=".js" />
      <mimeMap fileExtension=".js" mimeType="application/javascript" />

      <remove fileExtension=".mjs" />
      <mimeMap fileExtension=".mjs" mimeType="application/javascript" />

      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />

      <remove fileExtension=".css" />
      <mimeMap fileExtension=".css" mimeType="text/css" />

      <remove fileExtension=".svg" />
      <mimeMap fileExtension=".svg" mimeType="image/svg+xml" />

      <remove fileExtension=".webp" />
      <mimeMap fileExtension=".webp" mimeType="image/webp" />
    </staticContent>

    <httpProtocol>
      <customHeaders>
        <remove name="X-Powered-By" />
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="DENY" />
        <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
        <add name="Permissions-Policy" value="camera=(), microphone=(), geolocation=()" />
      </customHeaders>
    </httpProtocol>

  </system.webServer>
</configuration>
`;

const rewriteConfig = `<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <defaultDocument enabled="true"><files><clear /><add value="index.html" /></files></defaultDocument>
    <staticContent>
      <remove fileExtension=".js" /><mimeMap fileExtension=".js" mimeType="application/javascript" />
      <remove fileExtension=".mjs" /><mimeMap fileExtension=".mjs" mimeType="application/javascript" />
      <remove fileExtension=".json" /><mimeMap fileExtension=".json" mimeType="application/json" />
      <remove fileExtension=".css" /><mimeMap fileExtension=".css" mimeType="text/css" />
      <remove fileExtension=".svg" /><mimeMap fileExtension=".svg" mimeType="image/svg+xml" />
      <remove fileExtension=".webp" /><mimeMap fileExtension=".webp" mimeType="image/webp" />
    </staticContent>
    <rewrite>
      <rules>
        <rule name="OrcaFacil SPA fallback" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll"><add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" /><add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" /></conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
    <httpProtocol><customHeaders><remove name="X-Powered-By" /><add name="X-Content-Type-Options" value="nosniff" /><add name="X-Frame-Options" value="DENY" /><add name="Referrer-Policy" value="strict-origin-when-cross-origin" /><add name="Permissions-Policy" value="camera=(), microphone=(), geolocation=()" /></customHeaders></httpProtocol>
  </system.webServer>
</configuration>
`;

function publicationReadme() {
  return `OrçaFácil - Publicação IIS

Esta pasta dist está pronta para ser publicada no IIS.

Opção recomendada:
1. Copie a pasta dist para:
   C:\\inetpub\\wwwroot\\orcafacil

2. No IIS, crie um site ou aplicação apontando para:
   C:\\inetpub\\wwwroot\\orcafacil

3. Configure Documento Padrão:
   index.html

4. Acesse:
   http://localhost/orcafacil

Importante:
- Não abra index.html por file://
- O sistema precisa rodar por HTTP ou HTTPS
- Node.js é usado apenas para gerar esta pasta; o IIS não precisa de Node depois da publicação
- Adicione o domínio usado no Firebase Authentication > Authorized domains
- Se usar domínio próprio, configure HTTPS
- Acesse /instalacao.html para o assistente de instalação e checklist pós-publicação
- Acesse /diagnostico.html para diagnóstico técnico sem login
- O arquivo web.rewrite.config é opcional e só deve substituir web.config se o IIS tiver URL Rewrite instalado.
`;
}

async function writeGeneratedFiles() {
  await fs.writeFile(path.join(distDir, 'web.config'), webConfig);
  await fs.writeFile(path.join(distDir, 'web.rewrite.config'), rewriteConfig);
  await fs.writeFile(path.join(distDir, 'version.json'), JSON.stringify({
    app: 'OrçaFácil',
    version: pkg.version,
    buildDate: new Date().toISOString(),
    target: 'iis',
    mode: 'static',
    company: 'MNSOFT'
  }, null, 2) + '\n');
  await fs.writeFile(path.join(distDir, 'LEIA-ME-PUBLICACAO.txt'), publicationReadme());
}

async function validateDist() {
  const required = ['index.html', 'diagnostico.html', 'instalacao.html', 'web.config', 'version.json', path.join('css', 'app.css'), path.join('js', 'app.js'), path.join('js', 'diagnostico.js'), path.join('js', 'instalacao.js')];
  const missing = [];
  for (const rel of required) if (!await exists(path.join(distDir, rel))) missing.push(rel);
  if (missing.length) throw new Error(`Arquivos obrigatórios ausentes em dist: ${missing.join(', ')}`);

  const forbidden = [];
  for (const rel of forbiddenDistEntries) if (await exists(path.join(distDir, rel))) forbidden.push(rel);
  if (forbidden.length) throw new Error(`Arquivos/pastas proibidos encontrados em dist: ${forbidden.join(', ')}`);

  const sensitiveHits = [];
  for (const file of await walk(distDir)) {
    const rel = path.relative(distDir, file).replace(/\\/g, '/');
    if (isBlockedName(path.basename(file))) sensitiveHits.push(rel);
    const ext = path.extname(file).toLowerCase();
    if (!['.html', '.js', '.mjs', '.json', '.css', '.txt', '.config', '.svg'].includes(ext)) continue;
    const content = await fs.readFile(file, 'utf8');
    for (const term of sensitiveTerms) if (content.includes(term)) sensitiveHits.push(`${rel} (${term})`);
  }
  if (sensitiveHits.length) throw new Error(`Possíveis segredos encontrados em dist: ${[...new Set(sensitiveHits)].join(', ')}`);
}

async function createZipIfAvailable() {
  if (process.platform !== 'win32') {
    log('ZIP opcional não gerado neste ambiente. Pasta dist gerada com sucesso. Compacte manualmente se desejar.');
    return;
  }
  await ensureDir(publishDir);
  log('ZIP opcional pode ser gerado pelo publicar-iis.bat via PowerShell.');
}

async function main() {
  log('Limpando pasta dist...');
  await fs.rm(distDir, { recursive: true, force: true });
  await ensureDir(distDir);

  log('Copiando arquivos de public para dist...');
  await fs.copyFile(path.join(publicDir, 'index.html'), path.join(distDir, 'index.html'));
  for (const file of optionalRootFiles) {
    const source = path.join(publicDir, file);
    if (await exists(source)) await fs.copyFile(source, path.join(distDir, file));
  }
  for (const dir of copyDirs) await copyDirSafe(path.join(publicDir, dir), path.join(distDir, dir));

  log('Minificando CSS e JavaScript quando possível...');
  await minifyKnownAssets();

  log('Gerando web.config, version.json e LEIA-ME-PUBLICACAO.txt...');
  await writeGeneratedFiles();

  log('Validando publicação...');
  await validateDist();

  await createZipIfAvailable();
  log('Publicação IIS pronta em ./dist');
}

main().catch((error) => {
  console.error(`\nERRO NA PUBLICAÇÃO: ${error.message}`);
  process.exitCode = 1;
});
