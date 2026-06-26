# Deploy do OrçaFácil

## Secrets necessários
Configure em GitHub Secrets e/ou ambiente seguro das Functions: `FIREBASE_SERVICE_ACCOUNT_ORCAFACIL`, `FIREBASE_PROJECT_ID`, `MERCADO_PAGO_ACCESS_TOKEN`, `MERCADO_PAGO_WEBHOOK_SECRET`, `TELEGRAM_BOT_TOKEN`, `TELEGRAM_DEFAULT_CHAT_ID`. Não imprima secrets em logs.

## Local
```bash
npm install
npm run validate
npm run serve:dist
# Abra http://localhost:8095/instalacao.html e http://localhost:8095/diagnostico.html
```

## Firebase Hosting
```bash
firebase login
firebase use orcafacil-b771c
npm run validate
firebase deploy --only hosting
# Depois abra /instalacao.html e /diagnostico.html no domínio publicado
```

## Firestore Rules e Functions
```bash
npm run deploy:rules
npm run deploy:functions
npm run deploy
```

## IIS
1. Rode `npm run publish:iis` ou `publicar-iis.bat`.
2. Copie `dist` para `C:\MNSOFT\OrcaFacil\dist`.
3. Aponte o site IIS para `dist`, não para `public`.
4. Configure `index.html` como documento padrão e garanta `web.config`.
5. Abra `/instalacao.html`, execute o checklist pós-publicação e teste login, demo, PDF, histórico, `/diagnostico.html` e `/version.json`.

## Rollback
Firebase: `firebase hosting:releases:list` e `firebase hosting:rollback`; ou volte o commit anterior, rode `npm run build:prod` e `firebase deploy --only hosting`.
IIS: mantenha backup da pasta `dist`; renomeie a atual para `dist_failed`, restaure a anterior e recicle site/app pool.


## Assistente de instalação

A publicação inclui `instalacao.html`, uma página sem login para a equipe MNSOFT conferir ambiente, Firebase, domínio autorizado e comandos de publicação para Node local, IIS e Firebase Hosting. Use essa página após cada deploy antes de liberar o link ao cliente.

## Proteção contra `file://`

A aplicação principal e as páginas de apoio avisam quando abertas por `file://`. Sempre use HTTP/HTTPS porque ES Modules, Firebase Authentication, Firestore e geração de PDF dependem de um contexto web válido.
