const checks = [
  'Usuário comum lê apenas seus próprios documentos',
  'Usuário comum não altera role',
  'Usuário comum não altera plan',
  'Usuário comum não lê logs globais',
  'super_admin lê logs',
  'usuário bloqueado não cria documento',
  'publicQuotes não permite listagem pública'
];
console.log('Testes mínimos de Firestore Rules ainda estão em modo placeholder funcional. TODO: implementar com @firebase/rules-unit-testing.');
for (const check of checks) console.log(`TODO rules: ${check}`);
process.exit(0);
