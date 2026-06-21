const checks = [
  'createCheckoutPreference rejeita usuário não autenticado',
  'createCheckoutPreference não aceita preço vindo do front',
  'webhook não aprova pagamento inválido',
  'telegram function não envia sem token configurado'
];
console.log('Testes mínimos de Cloud Functions ainda estão em modo placeholder funcional. TODO: implementar testes com emulador/functions-test.');
for (const check of checks) console.log(`TODO functions: ${check}`);
process.exit(0);
