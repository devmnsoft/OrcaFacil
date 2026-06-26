import { detectEnvironment } from './core/environment.js';
import { firebaseConfig, APP_CHECK_ENABLED, APP_CHECK_SITE_KEY } from './firebase-config.js';

const env = detectEnvironment();
const environmentSummary = document.querySelector('#environmentSummary');
const firebaseSummary = document.querySelector('#firebaseSummary');
const domainStatus = document.querySelector('#domainStatus');
const currentDomain = document.querySelector('#currentDomain');
const fileAlert = document.querySelector('#fileAlert');
const deployCommands = document.querySelector('#deployCommands code');
const checklist = document.querySelector('#postChecklist');

function badge(label, value, status = 'ok') {
  const cls = status === 'ok' ? 'text-bg-success' : status === 'warn' ? 'text-bg-warning' : 'text-bg-danger';
  return `<div class="d-flex justify-content-between gap-3 border rounded-4 bg-white p-3"><span>${label}</span><span class="badge ${cls}">${value}</span></div>`;
}

function addFirebaseItem(label, value, status = 'ok') {
  const cls = status === 'ok' ? 'border-success' : status === 'warn' ? 'border-warning' : 'border-danger';
  firebaseSummary.insertAdjacentHTML('beforeend', `<div class="col-md-6"><div class="border ${cls} rounded-4 bg-white p-3 h-100"><div class="fw-bold">${label}</div><code class="small text-break">${value || 'não informado'}</code></div></div>`);
}

function expectedAuthHosts() {
  return new Set([
    'localhost',
    '127.0.0.1',
    firebaseConfig.authDomain,
    `${firebaseConfig.projectId}.web.app`,
    `${firebaseConfig.projectId}.firebaseapp.com`
  ].filter(Boolean));
}

function renderEnvironment() {
  environmentSummary.innerHTML = [
    badge('Protocolo', env.protocol, env.isFile ? 'error' : 'ok'),
    badge('Modo', env.mode, env.isFile ? 'error' : 'ok'),
    badge('Host', env.host || 'sem host', env.host ? 'ok' : 'warn'),
    badge('Base path', env.basePath || '/', 'ok')
  ].join('');
  currentDomain.textContent = env.hostname || '(sem domínio)';
  if (env.isFile) {
    fileAlert.classList.remove('d-none');
    fileAlert.innerHTML = '<strong>Não use file://.</strong> Rode <code>npm start</code> e acesse <code>http://localhost:8095</code>, ou publique em IIS/Firebase Hosting. ES Modules, Firebase Auth e Firestore precisam de HTTP/HTTPS.';
  }
}

function renderFirebase() {
  addFirebaseItem('Project ID', firebaseConfig.projectId, firebaseConfig.projectId ? 'ok' : 'error');
  addFirebaseItem('Auth domain', firebaseConfig.authDomain, firebaseConfig.authDomain ? 'ok' : 'error');
  addFirebaseItem('App ID', firebaseConfig.appId, firebaseConfig.appId ? 'ok' : 'warn');
  addFirebaseItem('Analytics', firebaseConfig.measurementId, firebaseConfig.measurementId ? 'ok' : 'warn');
  addFirebaseItem('App Check', APP_CHECK_ENABLED ? `Ativo (${APP_CHECK_SITE_KEY ? 'site key informada' : 'sem site key'})` : 'Desativado', APP_CHECK_ENABLED && !APP_CHECK_SITE_KEY ? 'warn' : 'ok');
}

function renderDomainStatus() {
  const allowed = expectedAuthHosts();
  const current = env.hostname;
  if (env.isFile) {
    domainStatus.className = 'alert alert-danger mb-0';
    domainStatus.innerHTML = 'Domínio não validável em <code>file://</code>. Use HTTP/HTTPS.';
    return;
  }
  if (allowed.has(current) || env.isLocalhost) {
    domainStatus.className = 'alert alert-success mb-0';
    domainStatus.innerHTML = 'Domínio compatível com a configuração padrão/local. Para domínio próprio, confirme o cadastro manual no Firebase Authorized domains.';
    return;
  }
  domainStatus.className = 'alert alert-warning mb-0';
  domainStatus.innerHTML = `Cadastre <code>${current}</code> no Firebase Authentication &gt; Settings &gt; Authorized domains antes de usar login em produção.`;
}

const commands = {
  node: `npm install\nnpm start\n# Acesse http://localhost:8095\n# Diagnóstico: http://localhost:8095/diagnostico.html`,
  iis: `npm install\nnpm run publish:iis\n# Windows: dê duplo clique em publicar-iis.bat\n# Copie o conteúdo de dist/ para C:\\inetpub\\wwwroot\\orcafacil\n# Acesse /instalacao.html e /diagnostico.html no IIS`,
  firebase: `npm install\nnpm run validate\nfirebase login\nfirebase use ${firebaseConfig.projectId || '<project-id>'}\nfirebase deploy --only hosting\n# Depois abra /instalacao.html e /diagnostico.html no domínio publicado`
};

document.querySelectorAll('.deploy-target').forEach((button) => {
  button.addEventListener('click', () => {
    document.querySelectorAll('.deploy-target').forEach((item) => item.classList.remove('active'));
    button.classList.add('active');
    deployCommands.textContent = commands[button.dataset.target];
  });
});

[
  ['HTTP/HTTPS ativo', !env.isFile],
  ['Domínio autorizado no Firebase Auth', expectedAuthHosts().has(env.hostname) || env.isLocalhost],
  ['diagnostico.html sem erros críticos', false],
  ['Login Firebase testado', false],
  ['Modo demonstração testado', false],
  ['PDF gerado com sucesso', false],
  ['Histórico persiste após atualizar', false],
  ['version.json acessível em produção', false]
].forEach(([text, checked], index) => {
  checklist.insertAdjacentHTML('beforeend', `<label class="form-check border rounded-4 bg-white p-3 ps-5"><input class="form-check-input" type="checkbox" ${checked ? 'checked' : ''} id="check-${index}"> ${text}</label>`);
});

renderEnvironment();
renderFirebase();
renderDomainStatus();
