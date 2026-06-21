import { APP_CONFIG } from './core/config.js';
import { detectEnvironment } from './core/environment.js';

const results = document.querySelector('#results');
const guidance = document.querySelector('#guidance');
function add(title, value, status = 'ok') {
  const cls = status === 'ok' ? 'text-success' : status === 'warn' ? 'text-warning' : 'text-danger';
  const label = status === 'ok' ? 'OK' : status === 'warn' ? 'Atenção' : 'Erro';
  results.insertAdjacentHTML('beforeend', `<div class="col-md-6"><div class="p-3 rounded-4 border bg-white h-100"><div class="fw-bold">${title}</div><div class="${cls}">${label} — ${value}</div></div></div>`);
}
(async function run(){
  const env = detectEnvironment();
  add('Aplicativo', `${APP_CONFIG.name} ${APP_CONFIG.version}`);
  add('URL atual', env.href);
  add('Protocolo', env.protocol, env.isFile ? 'error' : 'ok');
  add('Host', env.host || '(sem host)', env.host ? 'ok' : 'warn');
  add('Localhost', env.isLocalhost ? 'Sim' : 'Não');
  add('file://', env.isFile ? 'Detectado' : 'Não', env.isFile ? 'error' : 'ok');
  add('Parece IIS', env.isIISLike ? 'Sim/compatível' : 'Não identificado', 'ok');
  add('Parece Firebase Hosting', env.isFirebaseHosting ? 'Sim' : 'Não', 'ok');
  add('Base path', env.basePath);
  try { localStorage.setItem('orcafacil:diagnostico', new Date().toISOString()); add('localStorage', localStorage.getItem('orcafacil:diagnostico')); localStorage.removeItem('orcafacil:diagnostico'); } catch (err) { add('localStorage', err.message, 'error'); }
  try { await import('./core/config.js'); add('ES Module', 'Import dinâmico funcionando'); } catch (err) { add('ES Module', err.message, 'error'); }
  try { const fb = await import('./firebase-config.js'); add('Firebase config', `projectId ${fb.firebaseConfig?.projectId || 'não informado'}`, fb.firebaseConfig?.projectId ? 'ok' : 'warn'); } catch (err) { add('Firebase config', err.message, 'error'); }
  add('jsPDF', window.jspdf ? 'Disponível globalmente' : 'Será carregado na tela principal/CDN', window.jspdf ? 'ok' : 'warn');
  if (env.isFile) { guidance.className='alert alert-warning mt-4'; guidance.innerHTML='<strong>Não abra por file://.</strong> ES Modules e Firebase exigem HTTP/HTTPS. Use <code>npm start</code> e acesse <code>http://localhost:8095</code>, ou publique em IIS/Firebase Hosting.'; }
  else { guidance.className='alert alert-success mt-4'; guidance.innerHTML='<strong>Ambiente HTTP/HTTPS detectado.</strong> Se os itens críticos estão OK, valide login, modo demo e geração de PDF.'; }
})().catch(err => { console.error('diagnostico failed', err); add('Diagnóstico', err.message, 'error'); });
