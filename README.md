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
