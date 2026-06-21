# Release Checklist

## Antes do deploy
- [ ] `npm install`
- [ ] `npm run validate`
- [ ] `npm run security:check`
- [ ] Testar login, modo demo, PDF, Firestore Rules e Admin Geral.
- [ ] Testar pagamento sandbox e Telegram, se ativos.
- [ ] Verificar App Check, domínios autorizados Firebase Auth e secrets.
- [ ] Fazer backup do Firestore/dados administrativos/artifact `dist`.

## Depois do deploy
- [ ] Abrir URL produção.
- [ ] Testar login, criar orçamento, gerar PDF, plano Free e plano Pro.
- [ ] Verificar logs, erros, console, Telegram/logs e uso Firestore/Auth/Hosting.
- [ ] Registrar evento `SYSTEM_RELEASE_DEPLOYED` com version, environment, deployedAt, deployedBy, commitSha e notes.

## Rollback
- [ ] Identificar versão anterior.
- [ ] Reverter commit ou usar rollback do Firebase Hosting.
- [ ] Redeploy hosting e rules/functions se necessário.
- [ ] Registrar incidente e causa raiz.
