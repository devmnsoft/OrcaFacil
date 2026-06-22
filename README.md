# OrçaFácil

**Orçamentos e recibos profissionais em PDF, em segundos.**

OrçaFácil é um SaaS freemium para autônomos, MEIs e pequenos prestadores de serviço criarem orçamentos e recibos profissionais em PDF diretamente do navegador ou celular.

## Tecnologias

- JavaScript puro em módulos ES no navegador.
- Bootstrap 5 e Bootstrap Icons.
- Firebase Web SDK por CDN/module.
- Firebase Authentication com e-mail e senha.
- Cloud Firestore para usuários, perfil e documentos.
- Firebase Hosting.
- jsPDF e jsPDF autoTable para geração de PDFs.
- Node.js + Fastify local na porta **8095**.

## Como rodar localmente

```bash
npm install
npm start
```

Acesse: <http://localhost:8095>

O servidor local usa a porta **8095**.


## Importante: não abrir pelo file://

Não abra o OrçaFácil diretamente pelo caminho local, por exemplo `file:///C:/MNSOFT/OrcaFacil/public/index.html`. O projeto usa JavaScript puro com **ES Modules**, imports entre arquivos, Firebase SDK modular, jsPDF/autoTable e recursos do navegador que exigem uma origem HTTP/HTTPS confiável. Por segurança, navegadores modernos tratam páginas `file://` como origem `null` e bloqueiam imports locais, gerando erros de CORS.

Use sempre um servidor estático HTTP ou HTTPS. Exemplos corretos:

### Com Node/Fastify local

```bash
npm install
npm start
```

Acesse:

<http://localhost:8095>

Também é possível validar a URL explícita:

<http://localhost:8095/public/index.html>

### Com IIS apontando para `public`

Physical Path:

```text
C:\MNSOFT\OrcaFacil\public
```

Acesse:

<http://localhost/OrcaFacil>

### Com IIS apontando para a raiz

Physical Path:

```text
C:\MNSOFT\OrcaFacil
```

Acesse:

<http://localhost/OrcaFacil>

Nesse cenário, o `index.html` da raiz detecta `file://`, orienta o usuário quando necessário e, em HTTP/HTTPS, redireciona para `public/index.html`.

## Modo demonstração

No Windows, use também:

```bat
start.bat
```

## Firebase oficial configurado

O arquivo `public/js/firebase-config.js` já usa a configuração Web oficial do projeto:

```js
const firebaseConfig = {
  apiKey: "AIzaSyDfNFeiUSr8lq6UHZoQN6tR-Y_DkuWjVnw",
  authDomain: "orcafacil-b771c.firebaseapp.com",
  projectId: "orcafacil-b771c",
  storageBucket: "orcafacil-b771c.firebasestorage.app",
  messagingSenderId: "124049832916",
  appId: "1:124049832916:web:0f30944c6e2e8695e6f441",
  measurementId: "G-WXJGMB50K3"
};
```

> Essa configuração Web pode ficar no front-end. Não exponha service account, chave administrativa ou credenciais privadas do Firebase Admin.

## Como habilitar Authentication

1. Acesse o Firebase Console do projeto `orcafacil-b771c`.
2. Vá em **Authentication > Sign-in method**.
3. Habilite **E-mail/Senha**.
4. Salve.

## Como criar o Firestore Database

1. Acesse **Firestore Database** no Firebase Console.
2. Clique em **Create database**.
3. Para testes reais, escolha o modo de produção e publique as regras deste repositório.
4. Escolha uma região adequada para o público do projeto.

## Regras de segurança

As regras em `firestore.rules` garantem que cada usuário leia e escreva somente seus próprios dados:

```bash
firebase deploy --only firestore:rules
```

## Estrutura de dados

```text
users/{uid}
users/{uid}/settings/profile
users/{uid}/documents/{documentId}
```

### `users/{uid}`

```js
{
  uid,
  name,
  email,
  plan: "free",
  createdAt,
  updatedAt
}
```

### `users/{uid}/settings/profile`

```js
{
  businessName,
  documentNumber,
  phone,
  email,
  address,
  pix,
  logoBase64,
  updatedAt
}
```

### `users/{uid}/documents/{documentId}`

```js
{
  id,
  type: "orcamento" | "recibo",
  number: "ORC-000001" | "REC-000001",
  clientName,
  clientDocument,
  clientPhone,
  clientEmail,
  issueDate,
  dueDate,
  items: [],
  subtotal,
  discount,
  total,
  notes,
  status,
  createdAt,
  updatedAt
}
```

## Como testar usuário grátis

1. Abra `http://localhost:8095`.
2. Crie uma conta com e-mail e senha.
3. Salve os dados do emitente.
4. Crie um orçamento ou recibo.
5. Gere o PDF.
6. Confira a marca: **“Gerado com OrçaFácil — orçamentos e recibos profissionais em PDF”**.

## Como alterar plano para Pro manualmente

1. Acesse **Firestore Database**.
2. Abra `users/{uid}`.
3. Altere o campo `plan` para `pro`.
4. Recarregue o sistema e gere um PDF.
5. O PDF não deve exibir a marca OrçaFácil.


## Modo demonstração

O botão **Ver demonstração** ativa o modo localStorage. Ele mantém criação de perfil, documentos, histórico, duplicação, exclusão e PDF sem exigir login real no Firebase.

Cada usuário acessa apenas o próprio documento, conforme `firestore.rules`.

## Publicar no Firebase Hosting

O projeto já possui `firebase.json` com `public` apontando para `public` e rewrite para `index.html`.

```bash
firebase login
firebase use orcafacil-b771c
firebase deploy
```

Para publicar somente hosting:

```bash
firebase deploy --only hosting
```

## Checklist de teste recomendado

- Abrir `http://localhost:8095`.
- Criar conta.
- Fazer login e logout.
- Reabrir o navegador e validar sessão ativa.
- Salvar perfil do emitente.
- Criar orçamento.
- Criar recibo.
- Gerar PDF de orçamento e recibo.
- Ver histórico.
- Abrir/editar documento.
- Duplicar documento.
- Excluir documento.
- Validar plano free com marca no PDF.
- Alterar plano para `pro` no Firestore e validar PDF sem marca.
- Testar modo demonstração localStorage.
- Testar em tela mobile e verificar console sem erros.

## Roadmap futuro

Documentado para próximas etapas, ainda não implementado:

- Pagamento recorrente.
- Mercado Pago/Stripe.
- Envio automático por WhatsApp.
- Envio por e-mail.
- Aprovação online do orçamento pelo cliente.
- Conversão de orçamento aprovado em recibo.
- Upload real da logo no Firebase Storage.
- Dashboard financeiro.
- Multiusuário por conta.


## Funcionalidades comerciais desta versão

- Painel inicial com totais de documentos, totais por tipo, valores somados, plano atual e últimos 5 documentos.
- Tela **Minha assinatura** com comparativo Free x Pro, preços de **R$ 19,90/mês** e **R$ 199,00/ano**, e botão de WhatsApp para ativação com a MNSOFT.
- Histórico com busca por cliente, filtro por tipo, filtro por status, ordenação por data recente e ações de abrir, editar, duplicar, gerar PDF e excluir.
- Status do documento: `rascunho`, `emitido`, `aprovado` e `cancelado`.
- Exportação de backup em JSON e exportação CSV com número, tipo, cliente, data, status e total.
- Seção **Privacidade** em linguagem simples para usuários finais.

## Ativação manual do plano Pro

Enquanto o pagamento automático não está implementado, a ativação do Pro é feita manualmente no Firebase Console:

1. Acesse **Firebase Console > Firestore Database**.
2. Abra a coleção `users`.
3. Encontre o documento com o `uid` do usuário.
4. Altere ou crie o campo `plan` com o valor exato `pro`.
5. Clique em **Salvar**.
6. Peça para o usuário fazer logout/login ou atualizar a página.

No modo Firebase, o usuário não consegue se tornar Pro sozinho pela interface. No modo demonstração local, a tela **Minha assinatura** permite alternar Free/Pro apenas para teste visual e de PDF.

## Exportar backup

No menu **Histórico**, use:

- **Exportar meus documentos** para baixar `orcafacil-backup-AAAA-MM-DD.json` com os documentos carregados do usuário.
- **Exportar CSV** para baixar uma planilha simples com número, tipo, cliente, data, status e total.

No Firebase, a exportação usa os documentos carregados da subcoleção `users/{uid}/documents`. No modo demonstração, usa os documentos salvos no `localStorage`.

## LGPD e dados salvos

O OrçaFácil salva somente dados necessários para gerar documentos e manter histórico: dados do emitente, dados do cliente informados no documento, itens/serviços, valores, status e datas. Esses dados são usados para geração de PDFs e organização do histórico do próprio usuário. As regras do Firestore foram pensadas para cada usuário acessar apenas seus próprios dados. O usuário pode exportar seus documentos e pode solicitar exclusão da conta e dados entrando em contato com a MNSOFT.

## Próxima etapa recomendada

A próxima etapa do produto deve ser **pagamento e ativação automática do plano Pro**, provavelmente com Mercado Pago ou Stripe, mantendo a ativação manual como fallback administrativo.

## Aprovação pública de orçamento

A aprovação pública permite que o prestador gere um link para o cliente visualizar um orçamento sem login, baixar o PDF e registrar uma decisão.

### Como funciona

1. Crie e salve um documento do tipo **Orçamento**.
2. No histórico ou na tela do documento, clique em **Link de aprovação**.
3. O OrçaFácil gera ou reutiliza um token público seguro e grava os campos públicos no orçamento.
4. O modal mostra o link, ações para copiar, abrir e enviar pelo WhatsApp.
5. O cliente abre `/aprovar.html?t=TOKEN`, visualiza o orçamento, baixa o PDF e pode aprovar ou recusar.
6. A decisão aparece no histórico e no bloco **Resposta do cliente** ao abrir o orçamento no sistema interno.

### Estrutura `publicQuotes/{token}`

Ao gerar o link, é criado/atualizado um índice público no Firestore:

```js
{
  token: "token-unico-seguro",
  ownerUid: "uid-do-prestador",
  documentId: "id-do-orcamento",
  publicEnabled: true,
  createdAt: "2026-06-21T..."
}
```

A página pública usa esse índice para localizar `users/{ownerUid}/documents/{documentId}` e só exibe o documento se ele for um orçamento, estiver público e o token do documento bater com o token da URL.

### Campos adicionados ao documento

```js
{
  publicToken: "token-unico-seguro",
  publicEnabled: true,
  publicCreatedAt: "2026-06-21T...",
  publicLastAccessAt: "",
  clientDecision: "pendente", // pendente | aprovado | recusado
  clientDecisionAt: null,
  clientDecisionNote: "",
  issuerProfile: { /* snapshot do emitente para a página pública/PDF */ }
}
```

### Como testar localmente

```bash
npm install
npm start
```

Acesse <http://localhost:8095>, faça login ou use o modo demonstração, crie um orçamento, clique em **Link de aprovação** e abra o link local no formato:

```text
http://localhost:8095/aprovar.html?t=TOKEN
```

No modo demonstração, o link público é simulado com `localStorage` e funciona no mesmo navegador/perfil. Para compartilhar um link real com outra pessoa, use Firebase Authentication + Firestore + Hosting.

### Como testar no Firebase Hosting

1. Publique as regras do Firestore:

```bash
firebase deploy --only firestore:rules
```

2. Publique o Hosting:

```bash
firebase deploy --only hosting
```

3. Acesse o domínio do Firebase Hosting e gere um link no formato:

```text
https://DOMINIO_DO_FIREBASE/aprovar.html?t=TOKEN
```

### Como copiar, enviar e desativar link

- **Copiar link:** use o botão **Copiar link** no modal ou no histórico.
- **WhatsApp:** use **Enviar pelo WhatsApp** para abrir `https://wa.me/?text=...` com a mensagem sugerida.
- **Desativar link:** use **Desativar link**. O orçamento recebe `publicEnabled: false` e `publicQuotes/{token}` também recebe `publicEnabled: false`. A página pública passa a mostrar que o link não está mais disponível.

### Aprovar ou recusar como cliente

Na página pública, o cliente pode preencher uma mensagem opcional e clicar em:

- **Aprovar orçamento:** grava `clientDecision: "aprovado"`, `status: "aprovado"` e a data da decisão.
- **Recusar orçamento:** grava `clientDecision: "recusado"`, `status: "cancelado"` e a data da decisão.

### Limitações do MVP

- Não há envio automático de e-mail, WhatsApp ou push notification.
- A segurança pública evita listagem de documentos e limita atualizações públicas aos campos de decisão, mas documentos públicos podem ser lidos por caminho direto enquanto `publicEnabled` estiver ativo.
- O snapshot `issuerProfile` é gravado no orçamento ao gerar o link para permitir PDF público sem liberar leitura pública das configurações do usuário.
- O modo demonstração só simula link público no mesmo navegador.

### Próxima etapa recomendada

Implementar notificações para o prestador e um fluxo de conversão de orçamento aprovado em recibo, mantendo o MVP sem Cloud Functions até validar o uso real.

## Formas de rodar o OrçaFácil

### 1. Com Node local

```bash
npm install
npm start
```

Acesse:

- <http://localhost:8095>
- <http://localhost:8095/public/index.html> também funciona quando servido por um servidor estático apontado para a raiz.

### 2. Sem Node, usando servidor estático

A aplicação principal está em `public/` e pode ser servida por IIS, Apache, Nginx, Live Server, Firebase Hosting ou hospedagem comum. Use sempre um servidor HTTP; abrir `file://` pode bloquear módulos ES em alguns navegadores.

### 3. IIS apontando diretamente para `public` (recomendado para produção)

1. Abra o **IIS Manager**.
2. Crie um site ou aplicação para o OrçaFácil.
3. Configure **Physical Path** como:

   ```text
   C:\MNSOFT\OrcaFacil\public
   ```

4. Configure o documento padrão como `index.html`.
5. Garanta os MIME types:
   - `.js` `application/javascript`
   - `.mjs` `application/javascript`
   - `.json` `application/json`
   - `.css` `text/css`
6. Acesse: <http://localhost/OrcaFacil>

### 4. IIS apontando para a raiz do projeto

1. Abra o **IIS Manager**.
2. Crie um site ou aplicação com **Physical Path**:

   ```text
   C:\MNSOFT\OrcaFacil
   ```

3. Configure o documento padrão como `index.html`.
4. Acesse: <http://localhost/OrcaFacil>
5. O `index.html` da raiz redireciona para `public/index.html` quando a origem é HTTP/HTTPS.

**Recomendação:** em produção, aponte o IIS diretamente para `public`. Isso publica apenas os arquivos da aplicação e evita expor arquivos de desenvolvimento da raiz.

### MIME types no IIS

Os arquivos `web.config` da raiz e de `public/` configuram arquivos estáticos sem depender de ASP.NET:

- `.js` como `application/javascript`.
- `.mjs` como `application/javascript`.
- `.json` como `application/json`.
- `.css` como `text/css`.
- `index.html` como documento padrão.

### Diagnóstico de publicação

Após publicar, abra:

```text
/diagnostico.html
```

ou, se o IIS estiver apontando para a raiz:

```text
/public/diagnostico.html
```

A página mostra URL atual, protocolo, localhost, indícios de IIS/Firebase Hosting, teste de ES Module, Firebase config e `localStorage`, sem exigir login.

### Domínios autorizados no Firebase Auth

Firebase Authentication exige que o domínio esteja autorizado no Firebase Console:

1. Acesse **Firebase Console > Authentication > Settings > Authorized domains**.
2. Adicione o domínio do IIS, Apache, Nginx ou hospedagem usada.
3. Para testes locais, mantenha/adiciona `localhost`.
4. Para domínio próprio, adicione exatamente o domínio público usado pelos usuários.

## Arquitetura front-end modular

A arquitetura planejada está documentada em [`ARCHITECTURE.md`](./ARCHITECTURE.md). Nesta fase foram adicionadas as pastas `public/js/core`, `public/js/domain`, `public/js/services`, `public/js/repositories`, `public/js/ui` e `public/js/utils` para permitir migração gradual sem quebrar o MVP atual.

### Fases de evolução

- **Fase 1:** documentação, classes de domínio, utilitários e contratos de services/repositories mantendo compatibilidade com os arquivos atuais.
- **Fase 2:** migração progressiva de regras de negócio para services e repositories.
- **Fase 3:** separação das telas em módulos de UI e limpeza dos arquivos legados.

## Admin Geral, auditoria e monitoramento

Esta etapa adiciona uma camada administrativa leve ao OrçaFácil sem remover o modo demonstração, Firebase Authentication, Firestore, geração de PDF, histórico, plano Free/Pro, IIS/static hosting, Firebase Hosting ou servidor local na porta `8095`.

### Como ativar o primeiro `super_admin`

1. Faça login normalmente no OrçaFácil para criar o documento em `users/{uid}`.
2. Acesse o Firebase Console > Firestore.
3. Abra `users/{uid}` do Administrador Geral da MNSOFT.
4. Altere/adicone o campo:

```js
role = "super_admin"
```

5. Faça logout/login novamente. O menu **Admin Geral** aparecerá apenas para `role == "super_admin"`.

O documento do usuário passa a conter campos administrativos como `uid`, `name`, `email`, `plan`, `role`, `isActive`, `createdAt`, `updatedAt`, `lastLoginAt` e `lastSeenAt`. Usuários comuns não podem alterar `role` nem `plan` diretamente pelas regras do Firestore.

### Coleções de monitoramento

O MVP usa estas coleções no Firestore:

- `systemEvents/{eventId}`: eventos operacionais como cadastro, login, criação de documento, geração de PDF e exportações.
- `systemErrors/{errorId}`: erros de JavaScript, Firebase, permissões, PDF e promises rejeitadas.
- `auditLogs/{logId}`: trilha de auditoria das ações principais, salvando dados resumidos para LGPD.
- `adminSettings/global`: configurações administrativas e preferências de notificação Telegram, sem token.
- `telegramQueue/{messageId}`: fila segura de mensagens que serão enviadas pela Cloud Function.

### Serviços front-end adicionados

- `public/js/services/monitoring.service.js`: `trackEvent`, `trackError`, `audit`, `setUserContext` e captura global de `error`/`unhandledrejection`.
- `public/js/services/telegram-notification.service.js`: cria mensagens em `telegramQueue`, sem chamar a API do Telegram e sem token no navegador.
- `public/js/services/admin.service.js`: consultas e ações administrativas para dashboard, usuários, eventos, erros, auditoria, Telegram e saúde.
- `public/js/ui/admin.ui.js`: renderiza a aba **Admin Geral** com Dashboard, Usuários, Eventos, Erros/Bugs, Auditoria, Telegram e Saúde do sistema.

### Captura de erros e bugs

A aplicação registra automaticamente:

- erros globais de JavaScript;
- promises rejeitadas;
- falhas de permissão ou indisponibilidade do Firebase;
- falhas de geração de PDF;
- erros no carregamento do Admin Geral.

Erros técnicos não são exibidos em detalhe para usuários comuns. O super_admin pode visualizar mensagem, stack trace, URL, userAgent, contexto e marcar como resolvido com observação administrativa.

### Auditoria e LGPD

A auditoria cobre login, logout, criação/edição de documentos, geração de PDF, atualização do emitente e exportações JSON/CSV. Para reduzir exposição de dados pessoais, os logs priorizam identificadores e resumos como `documentId`, `documentNumber`, `type`, `total` e `status`, evitando salvar conteúdo detalhado de itens quando não necessário.

Senhas, tokens e credenciais nunca são armazenados pelo OrçaFácil nem exibidos na interface administrativa.

## Telegram seguro com Firebase Cloud Functions

O token do bot Telegram **não fica no front-end** e **não é salvo no Firestore**. O navegador apenas cria registros `pending` em `telegramQueue`; a Cloud Function `sendTelegramNotification` lê a fila e envia a mensagem usando variável de ambiente segura.

### Passo 1: criar bot

No Telegram, converse com **BotFather**, crie um bot e guarde o token em local seguro.

### Passo 2: obter `chat_id`

Envie uma mensagem para o bot e consulte `getUpdates` ou use um método equivalente para descobrir o `chat_id` do destino.

### Passo 3: configurar ambiente

Para Functions v2 com dotenv local, copie `functions/.env.example` para `functions/.env` e preencha:

```env
TELEGRAM_BOT_TOKEN=seu_token_seguro
TELEGRAM_DEFAULT_CHAT_ID=seu_chat_id
```

Não versione `functions/.env`.

Em projetos que ainda usam config clássica, também é possível manter o procedimento operacional documentado:

```bash
firebase functions:config:set telegram.bot_token="TOKEN"
firebase functions:config:set telegram.default_chat_id="CHAT_ID"
```

> Observação: a função implementada lê `TELEGRAM_BOT_TOKEN` e `TELEGRAM_DEFAULT_CHAT_ID` via ambiente/dotenv. Se optar por `functions:config`, ajuste a leitura conforme seu padrão de deploy.

### Passo 4: deploy das Functions

```bash
cd functions
npm install
cd ..
firebase deploy --only functions
```

### Passo 5: ativar no Admin Geral

1. Entre como `super_admin`.
2. Abra **Admin Geral > Telegram**.
3. Ative **Telegram ativo**.
4. Informe o `chat_id`.
5. Escolha os eventos que devem notificar.
6. Clique em **Enviar teste**.

A mensagem de teste cria um documento em `telegramQueue`. Após a Cloud Function processar, o status muda para `sent` ou `failed`.

### Eventos que podem notificar

- novo usuário cadastrado;
- novo orçamento ou recibo criado;
- PDF gerado, se habilitado;
- orçamento aprovado/recusado;
- erro crítico;
- login, se habilitado;
- exportação de dados, se habilitado;
- teste administrativo.

## Publicação das regras e hosting

Publique as regras do Firestore:

```bash
firebase deploy --only firestore:rules
```

Publique o hosting estático:

```bash
firebase deploy --only hosting
```

O projeto continua compatível com Firebase Hosting, IIS, Apache, Nginx ou qualquer servidor estático, pois a integração Telegram segura roda separadamente nas Cloud Functions.

## Testes sugeridos para aceite

1. `npm install`
2. `npm start`
3. Abrir `http://localhost:8095`
4. Entrar em modo demonstração e confirmar que documentos/PDF continuam funcionando.
5. Fazer login Firebase como usuário comum e confirmar que **Admin Geral** não aparece.
6. Criar orçamento/recibo, gerar PDF e exportar JSON/CSV.
7. Ativar `role = "super_admin"` no Firestore e fazer login novamente.
8. Abrir **Admin Geral** e conferir usuários, eventos, erros, auditoria e saúde.
9. Criar erro proposital em ambiente de teste e confirmar registro em **Erros/Bugs**.
10. Marcar erro como resolvido.
11. Alterar plano/role de usuário com confirmação.
12. Configurar Telegram, enviar teste e verificar `telegramQueue`.
13. Fazer deploy das Functions e confirmar status `sent`.
14. Publicar `firestore.rules` e testar acesso comum vs. super_admin.

## Limitações atuais do MVP Admin

- A proteção contra remoção do último `super_admin` está documentada, mas não totalmente automatizada no front-end; mantenha pelo menos uma conta `super_admin` ativa no Firestore.
- Métricas agregadas são calculadas por consultas recentes e limites de documentos para manter o SaaS leve; relatórios avançados/BI ficam para etapa futura.
- IP do usuário fica reservado como `ipInfo: null` para futura coleta segura no backend.

## Monitoramento Administrativo, Auditoria e Telegram

### Perfil Administrador Geral (`super_admin`)

O painel **Admin Geral** aparece somente para usuários autenticados cujo documento `users/{uid}` tenha `role = "super_admin"` e `isActive = true`. O primeiro Administrador Geral da MNSOFT deve ser ativado manualmente:

1. Acesse o Firebase Console > Firestore Database.
2. Abra a coleção `users` e o documento do usuário desejado (`users/{uid}`).
3. Adicione ou altere os campos:
   - `role`: `super_admin`
   - `isActive`: `true`
4. Faça logout/login no OrçaFácil.
5. A aba **Admin Geral** ficará disponível no menu principal.

Campos esperados em `users/{uid}`: `role` (`user`, `admin` ou `super_admin`), `isActive`, `lastLoginAt`, `lastSeenAt`, `createdAt` e `updatedAt`.

### Coleções de monitoramento

O MVP registra informações operacionais nas coleções:

- `systemEvents/{eventId}`: eventos do produto, como cadastro, login, criação/edição/exclusão/duplicação de documentos, geração de PDF, exportações, links públicos, mudança de plano e status de notificações Telegram.
- `systemErrors/{errorId}`: erros globais de JavaScript, rejeições de Promise, falhas de Firebase/Firestore, falhas de PDF e bugs críticos, com status de resolução e observação administrativa.
- `auditLogs/{logId}`: trilha de auditoria para ações relevantes sem salvar senhas, tokens, credenciais ou dados sensíveis desnecessários.
- `adminSettings/global`: preferências administrativas, incluindo Telegram ativo, Chat ID e eventos habilitados.
- `telegramQueue/{messageId}`: fila segura de mensagens. O front-end apenas cria mensagens pendentes; a Cloud Function envia ao Telegram.

Usuários comuns podem criar eventos/erros/auditorias próprios, mas não leem logs globais. O `super_admin` pode ler logs, usuários, configurações e alterar `plan`, `role` e `isActive`. Como este MVP ainda não usa custom claims, as regras consultam `users/{uid}.role`; publique as regras antes dos testes.

### Painel Admin Geral

Subáreas disponíveis:

- **Dashboard**: total de usuários, ativos, Free, Pro, documentos criados hoje, PDFs gerados hoje, orçamentos, recibos, erros 24h, críticos, Telegram pendente e último erro crítico.
- **Usuários**: lista nome, e-mail, plano, role, cadastro, último login e permite alterar plano, role e ativo/inativo com confirmação para alterações sensíveis.
- **Eventos**: lista eventos recentes.
- **Erros/Bugs**: lista erros, detalhes, stack trace, URL, navegador/contexto e permite marcar como resolvido com observação.
- **Auditoria**: exibe ações auditadas e diffs resumidos.
- **Telegram**: mostra Telegram ativo, Chat ID, eventos habilitados, fila, últimas mensagens/falhas e botão de mensagem teste.
- **Saúde do sistema**: ambiente detectado (Node local/localhost, Firebase Hosting ou IIS/static), Firebase/Auth/Firestore, fila Telegram, erros 24h e versão/estado operacional.

### Telegram seguro via Firebase Cloud Functions

O token do bot **nunca** deve ser salvo no front-end, README, Firestore ou arquivo público. Use somente variável segura/local das Functions.

Chat ID padrão configurado para notificações: `7535235489`.

Crie o bot no Telegram usando o **BotFather**, gere um novo token e crie o arquivo local não versionado:

```txt
functions/.env
```

Conteúdo local:

```env
TELEGRAM_BOT_TOKEN=NOVO_TOKEN_GERADO_NO_BOTFATHER
TELEGRAM_DEFAULT_CHAT_ID=7535235489
```

O repositório contém apenas `functions/.env.example`:

```env
TELEGRAM_BOT_TOKEN=coloque_aqui_o_token_do_bot
TELEGRAM_DEFAULT_CHAT_ID=7535235489
```

Instalação e deploy das Functions:

```bash
cd functions
npm install
firebase deploy --only functions
```

Publicação das regras e Hosting:

```bash
firebase deploy --only firestore:rules
firebase deploy --only hosting
```

A Cloud Function `sendTelegramNotification` monitora `telegramQueue/{messageId}`. Quando `status = "pending"`, ela lê `TELEGRAM_BOT_TOKEN` e `TELEGRAM_DEFAULT_CHAT_ID`, chama `https://api.telegram.org/bot{TOKEN}/sendMessage`, atualiza o status para `sent` ou `failed`, registra evento `TELEGRAM_NOTIFICATION_SENT` e grava falhas em `systemErrors`.

### Eventos Telegram padrão

Por padrão, o sistema pode notificar:

- Novo usuário cadastrado;
- Novo orçamento ou recibo criado;
- Orçamento aprovado ou recusado;
- Erro crítico;
- Falha de permissão no Firestore;
- Falha de geração de PDF;
- Alteração de plano para Pro;
- Desativação de link público.

Não há campo para token no Admin Geral. O Admin pode ativar/desativar Telegram, editar Chat ID, habilitar eventos e enviar mensagem teste.

### Testes recomendados

1. `npm install`
2. `npm start`
3. Abrir `http://localhost:8095`.
4. Criar conta e fazer login.
5. Ativar `super_admin` manualmente no Firestore e fazer logout/login.
6. Ver a aba **Admin Geral**.
7. Criar orçamento/recibo e gerar PDF.
8. Conferir `systemEvents` e `auditLogs`.
9. Gerar erro proposital em teste e conferir `systemErrors`.
10. Marcar erro como resolvido com observação.
11. Configurar `functions/.env` com o token seguro.
12. Fazer deploy das Functions.
13. No Admin Geral > Telegram, enviar mensagem teste.
14. Validar recebimento no Telegram.
15. Criar novo usuário/documento e validar notificações conforme flags.
16. Testar modo demonstração, IIS/static e Firebase Hosting.

### Segurança e LGPD

- Não versionar `functions/.env`, `.env`, tokens, senhas, Service Accounts ou chaves privadas.
- Logs e auditorias devem evitar dados sensíveis desnecessários.
- Senhas e credenciais nunca são registradas.
- O monitoramento existe para segurança, suporte, auditoria operacional e melhoria contínua.
- Usuários comuns acessam apenas seus próprios documentos; logs globais são restritos ao `super_admin`.

## Observabilidade, suporte e operação SaaS

### Logger centralizado
O front-end usa `public/js/services/logger.service.js` com os métodos `debug`, `info`, `success`, `warning`, `error`, `critical` e `audit`. O logger detecta ambiente (`localhost`, Node local, Firebase Hosting, IIS/static ou `file:`), captura usuário, URL, userAgent e grava em:

- `systemLogs`: linha operacional completa;
- `systemEvents`: eventos de negócio e sucesso/alerta;
- `systemErrors`: falhas técnicas visíveis ao `super_admin`;
- `auditLogs`: auditoria de ações importantes.

### Como funciona o logger

O logger trabalha em camadas para não quebrar o boot e não depender de permissões anônimas no Firestore:

1. **Antes do login:** os eventos aparecem no console e ficam em um buffer em memória, limitado aos logs mais recentes da sessão.
2. **Modo demonstração:** os registros são salvos somente no `localStorage` (`orcafacil:demo:*`) e não tentam escrever no Firestore.
3. **Após login:** quando `logger.setUserContext(user)` recebe um usuário com `uid`, os novos logs e os pendentes podem ser enviados para `systemLogs`, `systemEvents`, `systemErrors` e `auditLogs`.
4. **Permissões:** as Firestore Rules continuam exigindo autenticação para criar logs. Não existe escrita anônima em `systemLogs` ou auditoria.
5. **Leitura:** o `super_admin` lê logs globais pelo Admin Geral; usuários comuns não leem logs globais.

O logger possui proteção contra excesso de logs por sessão, deduplicação de eventos repetidos, warning controlado para `permission-denied` e remove campos sensíveis como senha, token, secret e apiKey. Logs de boot, como `APP_BOOT_START`, `ENVIRONMENT_DETECTED` e `APP_BOOT_SUCCESS`, não exigem Firestore para a aplicação iniciar.

### Try/catch global e erros
A inicialização instala handlers para `window.error` e `unhandledrejection`. Ações críticas como login, cadastro, salvamento, PDF, link público, perfil e exportações usam `try/catch` com mensagem amigável ao usuário e erro técnico no painel administrativo.

Para testar um erro proposital em desenvolvimento, abra o console do navegador e execute:

```js
setTimeout(() => { throw new Error('Teste proposital OrçaFácil'); }, 1000);
```

Depois acesse **Admin Geral > Erros/Bugs** com um usuário `super_admin`.

### Admin Geral: logs, bugs e auditoria
Somente usuários com `role: "super_admin"` em `users/{uid}` acessam **Admin Geral**. O painel lista usuários, logs, eventos, bugs/erros, auditoria, Telegram, saúde e configurações. Para ativar manualmente:

```json
{
  "role": "super_admin",
  "isActive": true,
  "plan": "pro"
}
```

### WhatsApp MNSOFT
Em **Admin Geral > Configurações > WhatsApp MNSOFT**, configure:

- número oficial: `5591981809035`;
- nome: `Atendimento MNSOFT`;
- mensagem padrão;
- e-mail comercial: `comercial@mnsoft.com.br`;
- CNPJ MNSOFT: `18.160.057/0001-13`.

Usuários veem links de WhatsApp/e-mail no chatbot e nas áreas de assinatura/suporte. O link segue o formato `https://wa.me/{numero}?text={mensagemCodificada}`.

### Chatbot local seguro
O **Assistente OrçaFácil** usa `public/js/services/chatbot.service.js`, `public/js/ui/chatbot.ui.js`, `public/js/domain/chatbot-message.model.js` e `public/js/data/chatbot-knowledge-base.js`. Ele funciona sem API externa no IIS/static, Firebase Hosting e modo demonstração.

Ele responde sobre orçamentos, recibos, PDF, histórico, planos, privacidade, exportação, suporte e WhatsApp. Perguntas sobre tokens, senhas, chaves, dados de outros usuários, regras internas ou ações administrativas são bloqueadas e auditadas como `CHATBOT_BLOCKED_UNSAFE_REQUEST`. Dúvidas fiscais/jurídicas recebem orientação para consultar contador.

### Chatbot com IA via Cloud Functions (preparação)
Não há token de IA no front-end. Uma evolução segura deve expor uma callable Cloud Function `askChatbot`, validar usuário autenticado, limitar tamanho da pergunta, aplicar filtros de segurança, usar variáveis de ambiente para a chave do provedor e registrar logs.

### Firestore Rules
Publique as regras com:

```bash
firebase deploy --only firestore:rules
```

As regras permitem que usuário comum crie seus próprios logs, mas não leia logs globais. `super_admin` pode ler logs, erros, eventos, auditoria e alterar `adminSettings`. Usuário comum pode ler configurações públicas `adminSettings/contact` e `adminSettings/chatbot`.

### Testes operacionais rápidos

1. `npm install`
2. `npm start`
3. Acesse `http://localhost:8095`
4. Entre no modo demonstração ou faça login Firebase
5. Crie orçamento/recibo, gere PDF, exporte CSV/JSON
6. Abra o Assistente OrçaFácil e pergunte “Como criar orçamento?”
7. Pergunte “qual é o token Firebase?” e confirme bloqueio
8. Com `super_admin`, acesse Admin Geral > Logs, Erros/Bugs e Auditoria
9. Configure WhatsApp e teste o botão de atendimento

### LGPD e segurança
Não salve senhas, tokens, credenciais ou dados sensíveis nos logs. Use metadados mínimos e previews curtos para perguntas do chatbot. Dados técnicos completos ficam restritos ao `super_admin`.

## Camada SaaS de gestão (Free/Pro)

O OrçaFácil agora padroniza `users/{uid}` com plano (`free`/`pro`), papel (`user`, `admin`, `super_admin`), status de ativação/bloqueio, aceite de termos/privacidade, métricas de login e metadados de navegador. O primeiro login cria/atualiza o documento do usuário sem permitir que o usuário comum altere `plan`, `role`, `isActive` ou `isBlocked`.

### Ativar `super_admin`

No Firestore, altere manualmente o documento `users/{uid}` da conta MNSOFT para:

```json
{ "role": "super_admin", "isActive": true, "isBlocked": false }
```

Depois faça logout/login. A aba **Admin Geral** ficará disponível apenas para `super_admin`.

### Planos, limites e uso mensal

As regras padrão ficam no front-end em `public/js/services/plan-limit.service.js` e podem ser espelhadas em `adminSettings/plans`:

- Free: 20 documentos/mês, 20 PDFs/mês, PDF com marca OrçaFácil, histórico operacional limitado.
- Pro: sem limites práticos no MVP, sem marca e com aprovação pública.

O uso mensal é salvo em `users/{uid}/usage/{yyyyMM}` com contadores de documentos, orçamentos, recibos, PDFs, links públicos, exportações, chatbot e última atividade. Para testar o limite Free, ajuste manualmente `documentsCreated` ou `pdfGenerated` para `20` no período atual (ex.: `202606`) e tente salvar/gerar PDF; o sistema mostra a mensagem de upgrade Pro e abre o fluxo de WhatsApp.

### Bloquear/desbloquear usuário

Em **Admin Geral > Usuários**, use **Bloquear** e informe o motivo. O login bloqueado não carrega o dashboard e mostra contatos da MNSOFT. Use **Desbloquear** para liberar novamente. Toda ação gera auditoria (`USER_BLOCKED_BY_ADMIN`, `USER_UNBLOCKED_BY_ADMIN`, `USER_DISABLED_BY_ADMIN`, `USER_ENABLED_BY_ADMIN`).

### Alterar plano e ativar Pro manual

Em **Admin Geral > Usuários**, altere o campo de plano ou use **Pro manual**. A ativação manual cria/atualiza `users/{uid}/billing/subscription` com `provider: "manual"`, `status: "active"` e `plan: "pro"`, registrando `SUBSCRIPTION_MANUAL_ACTIVATION`.

### Termos e privacidade

As páginas públicas ficam em `public/termos.html` e `public/privacidade.html`. No primeiro login real, o usuário precisa aceitar o modal obrigatório; o aceite grava `acceptedTermsAt` e `acceptedPrivacyAt` e registra auditoria `TERMS_ACCEPTED` e `PRIVACY_ACCEPTED`.

### WhatsApp, empresa e suporte

O contato padrão da MNSOFT usa `5591981809035` e `comercial@mnsoft.com.br`. Em **Admin Geral > Configurações**, o `super_admin` altera `adminSettings/contact`. A seção **Ajuda e Suporte** reúne chatbot, WhatsApp, e-mail, FAQ, termos e privacidade.

### Métricas e backup administrativo

**Admin Geral** mostra usuários, Free/Pro, documentos, PDFs, erros, eventos, auditoria e saúde. **Admin Geral > Backup** exporta CSV de usuários, logs, erros e auditoria sem senhas ou tokens e registra auditoria administrativa.

### Publicar Firestore Rules e Hosting

```bash
firebase deploy --only firestore:rules
firebase deploy --only hosting
```

As regras protegem dados por usuário, impedem alterações comuns em plano/papel/status, liberam configurações públicas autenticadas e restringem logs/auditoria/configurações sensíveis ao `super_admin`. Limitação do MVP: como ainda não há custom claims, a regra consulta `users/{uid}.role`; mantenha esse documento protegido e altere `role` apenas por administrador confiável.

### Testes recomendados de segurança e limites

1. `npm install`
2. `npm start`
3. Acesse `http://localhost:8095`
4. Crie usuário comum, aceite termos, crie orçamento/recibo e gere PDF.
5. Simule limite Free em `users/{uid}/usage/{yyyyMM}` e valide a mensagem de upgrade.
6. Ative `super_admin`, acesse Admin Geral, bloqueie/desbloqueie usuário, altere plano, exporte backup e confira auditoria.
7. Teste modo demonstração, publicação estática/IIS e Firebase Hosting.

### Preparação para pagamento futuro

A estrutura `users/{uid}/billing/subscription` já aceita provedores `manual`, `mercadopago` e `stripe`, status de assinatura, datas e IDs externos. A próxima etapa recomendada é pagamento manual/semiautomático do plano Pro com Mercado Pago.

## Formas de iniciar o OrçaFácil

### Windows — modo fácil
Dê duplo clique em `iniciar-windows.bat` ou execute:

```bat
scripts\iniciar-windows.bat
```

### Linux/macOS
```bash
chmod +x iniciar-linux-mac.sh
./iniciar-linux-mac.sh
```

### Terminal padrão
```bash
npm install
npm start
```
Acesse `http://localhost:8095`. O healthcheck fica em `http://localhost:8095/health` e o diagnóstico em `http://localhost:8095/diagnostico.html`.

### IIS recomendado
- Physical Path: `C:\MNSOFT\OrcaFacil\public`
- URL: `http://localhost/OrcaFacil`

### IIS alternativo
- Physical Path: `C:\MNSOFT\OrcaFacil`
- URL: `http://localhost/OrcaFacil`
- A raiz orienta/redireciona para `public/index.html`.

### Firebase Hosting
```bash
firebase deploy --only hosting
```

### Firebase Emulators
```bash
npm run emulators:start
```
Emulator UI: `http://localhost:4000`.

## Por que não abrir com file://
Abrir `public/index.html` diretamente via `file://` não é suportado. ES Modules, imports dinâmicos e Firebase SDK modular precisam de HTTP/HTTPS; navegadores podem bloquear imports por CORS. Use `npm start`, IIS ou Firebase Hosting.

## Diagnóstico
Abra `/diagnostico.html` para validar URL, protocolo, host, localhost, IIS, Firebase Hosting, localStorage, ES Modules, Firebase config e recomendações de correção.

## Scripts úteis
- `npm run check`: valida sintaxe do servidor.
- `npm run check:js`: valida sintaxe dos arquivos JavaScript locais.
- `npm run test:rules`: placeholder/base para testes de regras Firestore com emulador.
- `npm run test:functions`: placeholder/base para testes de functions com emulador.
- `npm run test:security`: executa regras e functions.

## Desenvolvimento x produção segura

- `public/` é a pasta de desenvolvimento usada por `npm start`.
- `dist/` é gerada por build e deve ser usada em produção para reduzir exposição do código original.
- Não abra o app por `file://`; use HTTP/HTTPS.

### Comandos

```bash
npm start
npm run build:prod
npm run security:check
npm run serve:dist
npm run deploy:rules
npm run deploy:hosting
```

### IIS

Em produção, aponte o IIS para `dist/` depois de executar `npm run build:prod`. O arquivo `web.config` é copiado para `dist/` com MIME types e headers de segurança básicos.

### Firebase Hosting

`firebase.json` publica `dist/`, aplica headers de segurança e usa fallback para `index.html`. Use `npm run deploy:hosting` para gerar build e publicar.

### App Check

O front-end possui configuração opcional em `public/js/firebase-config.js`. Em desenvolvimento, mantenha desligado. Em produção, registre o app no Firebase App Check, configure reCAPTCHA Enterprise/v3, valide domínios e só depois aplique enforcement.

### Limite da proteção de JavaScript

JavaScript no navegador não fica invisível nem criptografado de forma real. O build reduz exposição com minificação, remoção de comentários e ausência de source maps; segredos e lógica sensível devem ficar em Firestore Rules e Cloud Functions.

## Camada comercial: Mercado Pago e Plano Pro

O checkout do OrçaFácil é criado exclusivamente por Firebase Cloud Functions. O front-end chama `createCheckoutPreference`, recebe `init_point`/`sandbox_init_point` e redireciona o usuário; preço, plano e ativação nunca são definidos pelo navegador.

### Configurar `.env` das Functions

Copie `functions/.env.example` para `functions/.env` no ambiente local/CI seguro e configure:

```env
MERCADO_PAGO_ACCESS_TOKEN=seu_access_token_privado
MERCADO_PAGO_PUBLIC_KEY=public_key_opcional
MERCADO_PAGO_WEBHOOK_SECRET=segredo_para_validacao_futura
APP_BASE_URL=https://orcafacil-b771c.web.app
```

Nunca publique `functions/.env` nem coloque access token no front-end.

### Configurar Mercado Pago

1. Crie credenciais de sandbox/produção no painel Mercado Pago.
2. Cadastre as URLs de retorno:
   - `https://orcafacil-b771c.web.app/pagamento-sucesso.html`
   - `https://orcafacil-b771c.web.app/pagamento-pendente.html`
   - `https://orcafacil-b771c.web.app/pagamento-falha.html`
3. Cadastre a URL de webhook HTTPS da Function `mercadoPagoWebhook`.
4. Teste mensal (`R$ 19,90`) e anual (`R$ 199,00`) no sandbox.

### Fluxos de teste

- Criar checkout: abra **Minha assinatura**, clique em **Assinar mensal** ou **Assinar anual** e confirme redirecionamento ao Mercado Pago.
- Testar webhook: envie um payload de pagamento para `mercadoPagoWebhook`; a Function consulta a API Mercado Pago antes de ativar Pro.
- Validar ativação: pagamento `approved` atualiza `users/{uid}.plan = "pro"`, `users/{uid}/billing/subscription` e `users/{uid}/billing/payments/{paymentId}`.
- Validar rejeição: pagamento `rejected`/`cancelled` registra payment, mas não ativa Pro.
- Consultar pagamentos: usuário vê **Minha assinatura**; super_admin vê **Admin Geral > Assinaturas**.
- Ativar Pro manualmente: **Admin Geral > Usuários > Pro manual** ou **Assinaturas > Renovar**.

### Publicação

```bash
npm run build:prod
npm run security:check
firebase deploy --only functions
firebase deploy --only firestore:rules
firebase deploy --only hosting
```

A versão atual usa pagamento por período no MVP: mensal adiciona 30 dias e anual adiciona 365 dias. Recorrência automática avançada fica para uma etapa futura.

## Operação, build e deploy profissional

### Rodar localmente
- `npm install`
- `npm start` ou `npm run dev` na porta 8095.
- `npm run serve:dist` serve a pasta `dist` após o build.

### Validação e build
- `npm run validate` executa check de sintaxe, validação JS, build de produção e security check.
- `npm run build:prod` gera `dist` com arquivos minificados e `dist/version.json`.
- `npm run security:check` bloqueia arquivos proibidos, source maps e padrões sensíveis em `dist`.

### GitHub Actions
- `CI` roda em PR e push na `main`, valida o código, gera `dist` e publica artifact.
- `Firebase Preview` roda em PR e publica canal preview usando `FIREBASE_SERVICE_ACCOUNT_ORCAFACIL` e `FIREBASE_PROJECT_ID`.
- `Deploy Production` é apenas manual (`workflow_dispatch`) e permite escolher hosting, rules e functions.

### Secrets necessários
Configure sem expor em logs: `FIREBASE_SERVICE_ACCOUNT_ORCAFACIL`, `FIREBASE_PROJECT_ID`, `MERCADO_PAGO_ACCESS_TOKEN`, `MERCADO_PAGO_WEBHOOK_SECRET`, `TELEGRAM_BOT_TOKEN`, `TELEGRAM_DEFAULT_CHAT_ID`.

### Publicação
- Firebase: veja `DEPLOY.md`; hosting publica `dist`.
- IIS: produção deve usar `C:\MNSOFT\OrcaFacil\dist`; `public` é desenvolvimento.
- Rollback, backup e checklist estão em `DEPLOY.md`, `BACKUP.md` e `RELEASE_CHECKLIST.md`.

### Versão
Consulte `/version.json` no ambiente publicado e Admin Geral > Saúde.

## Aprovação pública de orçamentos

O OrçaFácil possui um fluxo comercial de aprovação pública para orçamentos. No histórico ou na tela do documento, use **Link de aprovação / Compartilhar** para gerar um token público imprevisível (`oqf_...`) e compartilhar por cópia, WhatsApp ou e-mail. A página `public/aprovar.html?t=TOKEN` abre sem login e lê apenas `publicQuotes/{token}`.

O cliente visualiza emitente, cliente, itens, validade, total e condições comerciais. Ao abrir um link válido, o sistema registra visualização, `viewCount`, `lastAccessAt` e timeline. O cliente pode aprovar com nome e aceite obrigatório ou recusar com justificativa. A decisão salva data/hora, navegador e um código SHA-256 de evidência.

Quando aprovado, o PDF do orçamento exibe o bloco **Aprovação do cliente** com status, aprovador, data, observação e evidência. Orçamentos aprovados podem ser convertidos em recibo pelo histórico; o recibo recebe `originBudgetId/originBudgetNumber` e o orçamento recebe `convertedReceiptId/convertedReceiptNumber` e status `convertido`.

### Aceite eletrônico simples

Este aceite eletrônico simples não substitui assinatura digital certificada ICP-Brasil, certificado digital, contrato formal ou orientação jurídica. Ele serve como evidência comercial operacional para autônomos, MEIs e pequenos prestadores.

### Teste rápido

1. Crie e salve um orçamento.
2. Gere o link público e copie a URL.
3. Abra a URL em aba anônima ou outro navegador.
4. Aprove com nome e checkbox ou recuse com motivo.
5. Verifique status, timeline, visualizações e código de evidência no histórico.
6. Gere o PDF aprovado e confira o bloco de aprovação.
7. Converta em recibo e confira o vínculo entre orçamento e recibo.

As regras do Firestore permitem `get` público somente por token em `publicQuotes/{token}`, bloqueiam listagem pública e restringem update público a visualização/decisão. O documento original em `users/{uid}/documents/{documentId}` continua privado.

## Logger e Observabilidade

O logger do OrçaFácil é resiliente e trabalha em quatro camadas para manter o boot silencioso e preservar as Firestore Rules seguras:

1. **Console:** em ambiente local (`localhost`/porta `8095`) os eventos de boot e erros aparecem no console para depuração.
2. **Buffer em memória:** antes do login, eventos como `APP_BOOT_START`, `ENVIRONMENT_DETECTED`, `FIREBASE_INIT_SUCCESS`, `AUTH_STATE_CHANGED` e `APP_BOOT_SUCCESS` ficam em memória, limitados por `maxPendingLogsBeforeLogin`.
3. **Modo demonstração:** com usuário demo ou flag `orcafacil:demo-enabled`, logs e auditoria são gravados em `localStorage` nas chaves `orcafacil:demo:*`; nada é enviado ao Firestore.
4. **Firestore autenticado:** após `logger.setUserContext(user)` receber `uid`, logs novos e pendentes recentes são enviados para `systemLogs`, `systemEvents`, `systemErrors` e `auditLogs`, sempre com `uid == request.auth.uid`.

O logger sanitiza `password`, `senha`, `token`, `secret`, `apiKey`, `authorization`, `privateKey`, `accessToken` e `refreshToken`; deduplica eventos repetitivos; limita logs por sessão; e trata `permission-denied` sem lançar exceção nem chamar `logger.error` internamente. A mensagem controlada esperada é: `[logger] Firestore recusou gravação de log neste contexto. O sistema continuará normalmente.`

### Como testar observabilidade

- **Sem login:** rode `npm start`, abra `http://localhost:8095` e confirme que o console mostra o boot sem `logger failed` e sem spam de `permission-denied`.
- **Modo demo:** clique em **Ver demonstração**, crie orçamento/recibo e gere PDF. Confira `localStorage` com chaves `orcafacil:demo:systemLogs`, `orcafacil:demo:systemEvents` e, se houver falha, `orcafacil:demo:systemErrors`.
- **Login real:** autentique com Firebase; o logger esvazia o buffer recente e persiste logs remotos conforme as rules.
- **Admin Geral > Logs:** filtre por nível, tipo, usuário, ambiente e mensagem/data; abra detalhes para metadados, URL, userAgent e erro; exporte CSV/JSON.
- **Admin Geral > Erros/Bugs:** veja erros não resolvidos, críticos e últimas 24h; copie detalhes, marque como resolvido com observação ou reabra.
- **Admin Geral > Saúde:** execute a visão de ambiente, Auth, Firestore, Logger, Telegram Queue, LocalStorage, versão, último crítico, erros 24h, logs pendentes, usuários ativos hoje e PDFs hoje.
- **Diagnóstico público:** abra `/diagnostico.html` para validar protocolo, ambiente, localStorage, ES Modules e Firebase config pública sem expor tokens, stack traces, rules ou dados de usuário.

### Publicação das Firestore Rules

As coleções globais de observabilidade não permitem escrita anônima. Publique as rules com:

```bash
firebase deploy --only firestore:rules
```

Se ocorrer `permission-denied`, não afrouxe segurança com `allow write: if true`. Verifique se o usuário está autenticado, se o documento contém `uid` igual ao `request.auth.uid`, se `level`/`severity` estão nos valores permitidos e se o usuário leitor é `super_admin`.

## Fluxo de autenticação e criação do usuário

O OrçaFácil separa o fluxo de Firebase Authentication do fluxo de dados do aplicativo no Cloud Firestore:

1. O Firebase Authentication cria e valida a credencial de e-mail e senha.
2. O documento `users/{uid}` guarda os dados operacionais do app, como plano, papel, status da conta, aceite de termos e metadados de sessão.
3. `role`, `plan`, `isActive`, `isBlocked` e contadores administrativos não são alteráveis pelo usuário comum após a criação inicial.
4. Erros de Auth e Firestore são tratados separadamente para não confundir senha incorreta com falha de permissão ou indisponibilidade do banco.
5. O monitoramento grava localmente em modo demonstração, faz buffer antes da autenticação e nunca deve quebrar login, cadastro ou onboarding.
6. Telegram e monitoramento remoto exigem usuário autenticado e configuração ativa; falhas de permissão são registradas como aviso controlado.

Para testar manualmente, valide: login inválido, cadastro com e-mail já existente, cadastro novo, falha simulada de `users/{uid}`, modo demonstração e login real com logs remotos permitidos pelas rules.

## Publicar no IIS com 1 clique

O OrçaFácil possui um publicador local para Windows que gera uma pasta `dist/` pronta para IIS estático.

### Como publicar

1. Baixe ou atualize o projeto.
2. Instale Node.js 18 ou superior.
3. Na raiz do projeto, dê duplo clique em:

```bat
publicar-iis.bat
```

O publicador verifica Node.js/npm, instala dependências quando `node_modules` não existir, limpa builds anteriores, gera `dist/`, cria `web.config`, valida arquivos obrigatórios, tenta criar um ZIP opcional em `publish/orcafacil-iis-dist.zip` e abre a pasta final.

Também é possível executar pelo terminal:

```bash
npm run publish:iis
```

### Onde publicar no IIS

Copie o conteúdo da pasta `dist/` para o IIS, por exemplo:

```text
C:\inetpub\wwwroot\orcafacil
```

Depois crie um site ou aplicação no IIS apontando para essa pasta e configure `index.html` como documento padrão.

### Testar a dist localmente

```bash
npm run serve:dist
```

Acesse:

<http://localhost:8095>

### Importante

- Não abra `index.html` por `file://`.
- O sistema precisa rodar por HTTP ou HTTPS.
- Node.js é necessário apenas para gerar `dist/`; depois disso, o IIS não precisa de Node.
- Adicione o domínio usado no Firebase Authentication em **Authorized domains**.

### Diferença entre `public/` e `dist/`

- `public/` é a pasta usada no desenvolvimento local e no fluxo atual do projeto.
- `dist/` é a pasta gerada para publicação IIS/produção estática.

Consulte o passo a passo detalhado em [`DEPLOY-IIS.md`](DEPLOY-IIS.md).
