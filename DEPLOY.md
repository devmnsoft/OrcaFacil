# Deploy do OrçaFácil

## Secrets necessários
Configure em GitHub Secrets e/ou ambiente seguro das Functions: `FIREBASE_SERVICE_ACCOUNT_ORCAFACIL`, `FIREBASE_PROJECT_ID`, `MERCADO_PAGO_ACCESS_TOKEN`, `MERCADO_PAGO_WEBHOOK_SECRET`, `TELEGRAM_BOT_TOKEN`, `TELEGRAM_DEFAULT_CHAT_ID`. Não imprima secrets em logs.

## Local
```bash
npm install
npm run validate
npm run serve:dist
```

## Firebase Hosting
```bash
firebase login
firebase use orcafacil-b771c
npm run deploy:hosting
```

## Firestore Rules e Functions
```bash
npm run deploy:rules
npm run deploy:functions
npm run deploy
```

## IIS
1. Rode `npm run build:prod`.
2. Copie `dist` para `C:\MNSOFT\OrcaFacil\dist`.
3. Aponte o site IIS para `dist`, não para `public`.
4. Configure `index.html` como documento padrão e garanta `web.config`.
5. Teste login, demo, PDF e `/version.json`.

## Rollback
Firebase: `firebase hosting:releases:list` e `firebase hosting:rollback`; ou volte o commit anterior, rode `npm run build:prod` e `firebase deploy --only hosting`.
IIS: mantenha backup da pasta `dist`; renomeie a atual para `dist_failed`, restaure a anterior e recicle site/app pool.
