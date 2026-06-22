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


## Segurança de logs e auditoria

- Logs anônimos não são gravados remotamente no Firestore; antes do login eles ficam no console e em buffer local de memória.
- No modo demonstração, logs e auditorias usam `localStorage` e não criam registros remotos.
- Escritas em `systemLogs`, `systemEvents`, `systemErrors` e `auditLogs` exigem usuário autenticado pelas Firestore Rules.
- Logs nunca devem conter tokens, senhas, secrets, chaves privadas ou credenciais de integração.
- Stack traces e detalhes técnicos não devem ser exibidos para usuário comum; mensagens de interface devem ser amigáveis.
- Usuários `super_admin` podem visualizar erros técnicos, eventos e auditoria no Admin Geral para suporte, segurança e observabilidade.

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

## Segurança financeira e Mercado Pago

- O `MERCADO_PAGO_ACCESS_TOKEN` deve existir somente em Firebase Cloud Functions/variáveis seguras.
- O front-end nunca define preço final, nunca chama a API Mercado Pago com credencial privada e nunca ativa Pro diretamente.
- A Function `createCheckoutPreference` busca o preço em `adminSettings/plans` ou usa fallback seguro de servidor.
- O webhook `mercadoPagoWebhook` deve consultar a API Mercado Pago antes de aprovar qualquer pagamento.
- Dados sensíveis de cartão, token e identificação do pagador não devem ser persistidos; payloads de webhook são sanitizados em `paymentWebhooks`.
- Escritas críticas em billing devem ocorrer preferencialmente via Cloud Functions/Admin SDK; Firestore Rules bloqueiam escrita direta de usuários comuns.
- Eventos financeiros devem gerar `systemEvents`, `auditLogs`, `systemErrors` em falhas críticas e, quando habilitado, fila Telegram.

## Segurança de CI/CD e releases

- O CI executa `npm run security:check` após `npm run build:prod`.
- A pasta `dist` não deve conter `.env`, source maps, documentação interna, código de testes ou tokens.
- Segredos ficam somente em GitHub Secrets, Firebase environment config, `.env` local não versionado ou variáveis de Cloud Functions.
- Deploy de produção é manual por GitHub Actions e deve ser precedido por backup e checklist de release.
- Vulnerabilidades devem ser reportadas para `comercial@mnsoft.com.br` com passos de reprodução e impacto.

## Segurança do link público de orçamento

Os links públicos usam tokens longos e imprevisíveis gerados por `crypto.randomUUID()` ou `crypto.getRandomValues()`, com prefixo `oqf_`. A página pública lê somente `publicQuotes/{token}` e nunca acessa diretamente `users/{uid}/documents/{documentId}`.

`publicQuotes` deve conter apenas os dados necessários para visualização comercial do orçamento. Dados administrativos, plano, papéis, billing, logs internos e outros documentos não são expostos. As regras Firestore bloqueiam listagem pública e restringem atualizações anônimas aos campos de visualização e decisão.

Entradas públicas são limitadas, sanitizadas para reduzir risco de XSS e registram evidências mínimas: data, nome informado, navegador e hash SHA-256. O aceite é simples/comercial e não substitui assinatura digital certificada ICP-Brasil.

## Segurança de logs e observabilidade

- Logs remotos exigem Firebase Authentication e `uid == request.auth.uid`; não há escrita anônima em `systemLogs`, `systemEvents`, `systemErrors` ou `auditLogs`.
- Usuários comuns não leem logs globais. A leitura e atualização administrativa ficam restritas a `super_admin`.
- O logger sanitiza campos sensíveis (`password`, `senha`, `token`, `secret`, `apiKey`, `authorization`, `privateKey`, `accessToken`, `refreshToken`) antes de persistir console/localStorage/Firestore.
- Stack trace e detalhes técnicos ficam no Admin Geral; o diagnóstico público não exibe tokens, rules, dados de usuário, logs internos ou stack traces.
- `permission-denied` em logs é tratado como falha observável controlada: o app continua, registra fallback local quando possível e não entra em loop.
