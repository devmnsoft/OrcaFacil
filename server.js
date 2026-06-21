import http from 'node:http';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import process from 'node:process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const PORT = Number(process.env.PORT || 8095);
const HOST = process.env.HOST || '0.0.0.0';
const PUBLIC_DIR = process.argv.includes('--dist') ? 'dist' : (process.env.PUBLIC_DIR || 'public');
const ROOT_DIR = path.resolve(__dirname, PUBLIC_DIR);
const packageJson = JSON.parse(await fs.readFile(path.join(__dirname, 'package.json'), 'utf8'));

const MIME_TYPES = new Map([
  ['.html', 'text/html; charset=utf-8'],
  ['.css', 'text/css; charset=utf-8'],
  ['.js', 'application/javascript; charset=utf-8'],
  ['.mjs', 'application/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.svg', 'image/svg+xml'],
  ['.png', 'image/png'],
  ['.jpg', 'image/jpeg'],
  ['.jpeg', 'image/jpeg'],
  ['.webp', 'image/webp'],
  ['.ico', 'image/x-icon'],
  ['.pdf', 'application/pdf'],
  ['.txt', 'text/plain; charset=utf-8']
]);

function send(res, statusCode, body, headers = {}) {
  res.writeHead(statusCode, headers);
  res.end(body);
}

function json(res, statusCode, payload) {
  send(res, statusCode, JSON.stringify(payload, null, 2), { 'content-type': 'application/json; charset=utf-8' });
}

function safePath(urlPathname) {
  const decoded = decodeURIComponent(urlPathname).replace(/\\/g, '/');
  const normalized = path.posix.normalize(decoded).replace(/^\/+/, '');
  const absolute = path.resolve(ROOT_DIR, normalized);
  const rootWithSep = ROOT_DIR.endsWith(path.sep) ? ROOT_DIR : `${ROOT_DIR}${path.sep}`;
  if (absolute !== ROOT_DIR && !absolute.startsWith(rootWithSep)) return null;
  return absolute;
}

async function resolveFile(requestPath) {
  const absolute = safePath(requestPath);
  if (!absolute) return { forbidden: true };
  try {
    const stat = await fs.stat(absolute);
    if (stat.isDirectory()) return { file: path.join(absolute, 'index.html') };
    if (stat.isFile()) return { file: absolute };
  } catch {}
  return { file: path.join(ROOT_DIR, 'index.html'), fallback: true };
}

async function handler(req, res) {
  const start = Date.now();
  const method = req.method || 'GET';
  try {
    const url = new URL(req.url || '/', `http://${req.headers.host || `localhost:${PORT}`}`);
    if (url.pathname === '/health') {
      return json(res, 200, { status: 'ok', app: 'orcafacil', version: packageJson.version, port: PORT, publicDir: PUBLIC_DIR, time: new Date().toISOString() });
    }
    if (!['GET', 'HEAD'].includes(method)) return json(res, 405, { error: 'method_not_allowed' });

    const result = await resolveFile(url.pathname);
    if (result.forbidden) return json(res, 403, { error: 'forbidden' });
    const data = await fs.readFile(result.file);
    const ext = path.extname(result.file).toLowerCase();
    const contentType = MIME_TYPES.get(ext) || 'application/octet-stream';
    const headers = { 'content-type': contentType, 'x-content-type-options': 'nosniff' };
    res.writeHead(result.fallback ? 200 : 200, headers);
    if (method === 'HEAD') return res.end();
    return res.end(data);
  } catch (err) {
    console.error(`[server] erro em ${method} ${req.url}:`, err);
    return json(res, 500, { error: 'internal_server_error' });
  } finally {
    res.on('finish', () => console.log(`${method} ${req.url} ${res.statusCode} ${Date.now() - start}ms`));
  }
}

const server = http.createServer(handler);
server.listen(PORT, HOST, () => {
  console.log('========================================');
  console.log('OrçaFácil iniciado com sucesso');
  console.log('Ambiente: local');
  console.log(`Pasta pública: ./${PUBLIC_DIR}`);
  console.log(`URL local: http://localhost:${PORT}`);
  console.log(`Healthcheck: http://localhost:${PORT}/health`);
  console.log(`Diagnóstico: http://localhost:${PORT}/diagnostico.html`);
  console.log('========================================');
});
