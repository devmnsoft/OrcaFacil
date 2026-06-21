# Arquitetura do OrçaFácil

## Visão geral
O OrçaFácil é uma aplicação SaaS freemium essencialmente estática, escrita em JavaScript puro com módulos ES, Bootstrap 5, Firebase Authentication, Firestore, Hosting e geração de PDF no navegador.

## Formas de execução
- **Node local**: `npm start` executa `server.js`, um servidor `node:http` sem Fastify, servindo `public` por padrão e expondo `/health`.
- **IIS/static**: recomendado apontar o Physical Path para `public`. A raiz contém apenas uma orientação/redirecionamento para `public/index.html`.
- **Firebase Hosting**: `firebase.json` mantém `public` como pasta pública e rewrite para `/index.html`.
- **Firebase Emulators**: portas padronizadas para Auth, Firestore, Functions, Hosting e Emulator UI.

## Estrutura de pastas
- `public/js/core`: configuração, ambiente, bootstrap e rotas.
- `public/js/domain`: modelos de domínio.
- `public/js/services`: integrações, regras de negócio, PDF, logger, auditoria, chatbot e admin.
- `public/js/repositories`: acesso a dados Firebase/localStorage.
- `public/js/ui`: componentes de tela.
- `public/js/utils`: utilitários puros, validações, erros e action guard.

## Fluxo de inicialização
`app.js` chama `bootstrapApp`, que detecta o ambiente, bloqueia `file://` com mensagem amigável, registra logs de boot e inicia os serviços existentes.

## Fluxo de autenticação
O app tenta usar Firebase Authentication quando disponível; o modo demonstração continua usando localStorage para permitir validação sem conta real.

## Fluxo de documentos e PDF
Documentos são coletados pela UI, validados, numerados, salvos via service/repository e enviados ao gerador PDF. O plano Free mantém marca no PDF; o Pro remove a marca.

## Fluxo de logs e chatbot
O logger grava no console, no Firestore quando logado e no localStorage em modo demo, sem interromper o app caso a persistência falhe. O chatbot local continua isolado da inicialização crítica.

## Padrões de try/catch e action guard
Ações críticas devem usar `withTryCatch`, `withButtonLoading`, `preventDoubleClick` e `rateLimit` em `public/js/utils/action-guard.js` para impedir duplo clique, spam e erros sem feedback.

## Evolução segura
Migrar funcionalidades gradualmente das telas grandes para módulos menores; manter compatibilidade de exports existentes; criar testes de regras/functions antes de endurecer segurança; nunca remover login, demo, Firestore, PDF, histórico, chatbot, logger, admin ou compatibilidade estática sem etapa dedicada.

## Arquitetura comercial e billing

### Estruturas Firestore

- `adminSettings/plans`: configuração global dos planos Free/Pro.
- `users/{uid}/billing/subscription`: assinatura corrente, status, ciclo, datas e IDs externos.
- `users/{uid}/billing/payments/{paymentId}`: histórico de pagamentos Mercado Pago.
- `paymentWebhooks/{webhookId}`: registro idempotente e sanitizado de webhooks recebidos.

### Fluxo de checkout

1. Usuário autenticado abre **Minha assinatura** e escolhe ciclo mensal/anual.
2. Front-end chama `createCheckoutPreference` sem enviar preço confiável.
3. Cloud Function valida usuário, bloqueio, plano/ciclo e busca preço no servidor.
4. Function cria preferência Mercado Pago, grava assinatura/payment `pending` e retorna URL de checkout.

### Fluxo de webhook e ativação Pro

1. Mercado Pago chama `mercadoPagoWebhook`.
2. Function registra webhook sanitizado e consulta o pagamento real na API Mercado Pago.
3. Status `approved` atualiza payment, assinatura `active`, `users/{uid}.plan = "pro"` e auditoria.
4. Status não aprovado registra histórico e eventos, mas não altera o plano para Pro.
5. Processamento duplicado de pagamento aprovado é ignorado com idempotência.

### Expiração e administração

`checkExpiredSubscriptions` roda diariamente e rebaixa assinaturas vencidas para Free. O super_admin consulta assinaturas, pagamentos, webhooks e pode ativar, renovar, cancelar ou voltar usuários para Free com auditoria.
