# Segurança do OrçaFácil

## Proteções implementadas

- `public/` é ambiente de desenvolvimento; `dist/` é gerado para produção.
- O build de produção remove comentários, minifica JavaScript/CSS e não gera source maps.
- Firebase Hosting publica `dist/` e aplica headers de segurança.
- IIS pode apontar para `dist/` usando `web.config` com MIME types e headers básicos.
- Firestore Rules restringem dados por usuário, bloqueiam alteração comum de `role`, `plan`, `isActive` e `isBlocked`, e limitam logs.
- Orçamentos públicos usam `publicQuotes/{token}` sanitizado; documentos originais em `users/{uid}/documents/{id}` não têm leitura pública direta.

## JavaScript não pode ser criptografado de verdade

Todo JavaScript executado no navegador pode ser baixado, inspecionado e depurado. A proteção prática é minificar, ofuscar quando a dependência estiver disponível, remover comentários/source maps e, principalmente, não colocar segredos nem decisões sensíveis no front-end.

## Segredos

Nunca coloque no front-end: Admin SDK, service account, `private_key`, tokens Telegram/IA/WhatsApp, senhas, chaves de pagamento ou secrets de APIs. A configuração Web do Firebase contém `apiKey` pública; ela não é senha. A segurança depende de Firestore Rules, domínios autorizados, App Check e Cloud Functions para operações sensíveis.

## App Check

O projeto está preparado em `public/js/firebase-config.js` com `APP_CHECK_ENABLED = false` e `APP_CHECK_SITE_KEY = ""`. Para produção: registre o app Web no Firebase App Check, escolha reCAPTCHA Enterprise ou v3, configure domínios autorizados, preencha a site key pública, teste sem enforcement e só depois aplique enforcement no Firestore.

## Publicação

- Desenvolvimento: `npm start` e acesse `http://localhost:8095`.
- Build seguro: `npm run build:prod`.
- Teste local do build: `npm run serve:dist`.
- Firebase Hosting: `npm run deploy:hosting`.
- Firestore Rules: `npm run deploy:rules`.
- IIS: gere `dist/` e aponte o site para essa pasta.

## Checklist de produção

1. Rodar `npm run build:prod`.
2. Rodar `npm run security:check`.
3. Confirmar que `dist/` não tem `.env`, source maps, docs internas ou scripts.
4. Publicar Firestore Rules.
5. Testar login, demo, PDF, histórico, Admin Geral e link público por token.
6. Ativar App Check gradualmente.
7. Mover integrações sensíveis para Cloud Functions.

## Reporte de vulnerabilidades

Envie detalhes técnicos, URL afetada, impacto e passos de reprodução para o canal oficial de suporte da MNSOFT/OrçaFácil.
