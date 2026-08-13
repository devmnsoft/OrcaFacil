(function () {
  'use strict';
  let count = 0;
  const report = (kind, details) => { count += 1; console.error(`[OrçaFácil ${kind} #${count}]`, details); };
  window.addEventListener('error', event => report('JS Error', { message: event.message, source: event.filename, line: event.lineno, column: event.colno }));
  window.addEventListener('unhandledrejection', event => report('Promise Error', { reason: String(event.reason?.message || event.reason || 'Erro sem detalhe') }));
  window.OrcaJsHealth = Object.freeze({ get errorCount() { return count; } });
})();
