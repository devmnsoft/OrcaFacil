import { logger } from '../services/logger.service.js';
import { detectEnvironment } from './environment.js';
export async function bootstrapApp(start) {
  const env = detectEnvironment();
  await logger.info('APP_BOOT_START', 'Inicialização solicitada', env);
  if (env.isFile) {
    document.body.innerHTML = '<main class="container py-5"><div class="alert alert-warning"><h1>Abra o OrçaFácil por HTTP/HTTPS</h1><p>O acesso por file:// não é suportado porque ES Modules e Firebase precisam de um servidor web.</p><p>Use <code>npm start</code> e acesse <code>http://localhost:8095</code>, ou publique no IIS/Firebase Hosting.</p><a class="btn btn-primary" href="diagnostico.html">Abrir diagnóstico</a></div></main>';
    await logger.warning('SERVER_STATIC_MODE_DETECTED', 'file:// detectado; app completo não iniciado', env);
    return;
  }
  await logger.info('ENVIRONMENT_DETECTED', 'Ambiente detectado', env);
  try { await start(env); await logger.success('APP_BOOT_SUCCESS', 'Aplicação iniciada com sucesso', env); }
  catch (err) { await logger.critical('APP_BOOT_ERROR', 'Falha ao iniciar aplicação', err, env); throw err; }
}
