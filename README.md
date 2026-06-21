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

### 3. IIS apontando para a raiz do projeto

1. Copie todo o projeto para `C:\inetpub\wwwroot\orcafacil`.
2. Garanta que o `index.html` da raiz e o `web.config` estejam presentes.
3. Configure **Documento Padrão** para `index.html`.
4. Acesse `http://servidor/orcafacil`.
5. A entrada da raiz redireciona para `public/index.html`.

### 4. IIS apontando diretamente para `public`

1. Copie a pasta `public` para `C:\inetpub\wwwroot\orcafacil` ou aponte o site diretamente para essa pasta.
2. Configure **Documento Padrão** para `index.html`.
3. Acesse `http://servidor/orcafacil`.

**Recomendação:** para produção estática simples no IIS, aponte o site diretamente para a pasta `public`, pois ela contém a aplicação real e evita redirecionamento intermediário.

### MIME types no IIS

Se o IIS bloquear módulos ES, arquivos `.js` ou `.json`, confirme os MIME types:

- `.js` como `application/javascript`.
- `.mjs` como `application/javascript`.
- `.json` como `application/json`.

O `web.config` da raiz já inclui uma configuração estática para esses tipos e um fallback básico para `public/index.html`. Ele não depende de ASP.NET.

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
