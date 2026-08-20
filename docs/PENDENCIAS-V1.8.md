# Pendências reais da V1.8

## Push notifications

Push web não foi habilitado nesta versão. A interface não mostra controle de push e o sistema continua usando as notificações internas existentes. A implementação futura depende de provedor Web Push, chaves VAPID fornecidas exclusivamente por configuração segura, consentimento e revogação por usuário, tratamento de endpoints expirados e validação operacional em navegadores compatíveis.

## Rascunhos offline

Não há sincronização nem persistência de rascunhos offline. Enquanto estiver sem conexão, a V1.8 bloqueia submissões e informa de modo explícito que a operação precisa de internet. Rascunhos locais somente devem ser considerados depois de uma classificação formal dos dados permitidos e de uma etapa obrigatória de revisão antes do envio.
