# OrçaFácil

**Orçamentos e recibos profissionais em PDF, em segundos.**

Projeto MVP completo com front-end Bootstrap + JavaScript, Firebase Authentication/Firestore e servidor local em Node/Fastify.

## O que vem pronto

- Landing page comercial.
- Login e cadastro por e-mail/senha.
- Modo demonstração local quando o Firebase ainda não está configurado.
- Cadastro do emitente: nome, CPF/CNPJ, contato, Pix, cidade, logo e plano.
- Criação de orçamento e recibo.
- Itens com quantidade, valor unitário, desconto e total automático.
- Validade e observações para orçamento.
- Recibo com valor por extenso.
- Histórico de documentos para abrir, editar, duplicar e reemitir PDF.
- Numeração sequencial por usuário.
- PDF profissional com logo, dados do emitente, cliente, itens e rodapé freemium.
- Servidor local na porta **8095**.
- Arquivos de deploy Firebase Hosting e regras Firestore.

## Como rodar localmente

1. Instale Node.js 18 ou superior.
2. Extraia o ZIP.
3. Abra o terminal na pasta do projeto.
4. Execute:

```bash
npm install
npm start
```

Acesse:

```text
http://localhost:8095
```

No Windows, também pode dar duplo clique em `start.bat`.

## Configurar Firebase

Abra `public/js/firebase-config.js` e cole a configuração do seu projeto Firebase:

```js
export const firebaseConfig = {
  apiKey: "SUA_API_KEY",
  authDomain: "SEU_PROJETO.firebaseapp.com",
  projectId: "SEU_PROJETO",
  storageBucket: "SEU_PROJETO.appspot.com",
  messagingSenderId: "000000000000",
  appId: "1:000000000000:web:000000000000"
};
```

Depois habilite no Firebase:

- Authentication > Sign-in method > E-mail/Senha.
- Firestore Database.
- Hosting, caso deseje publicar.

## Deploy no Firebase Hosting

```bash
npm install -g firebase-tools
firebase login
firebase init hosting firestore
firebase deploy
```

Este projeto já inclui `firebase.json` e `firestore.rules`.

## Estrutura

```text
orcafacil/
  public/
    index.html
    css/app.css
    js/app.js
    js/firebase-config.js
    js/services.js
    js/pdf.js
    js/utils.js
  server.js
  package.json
  firebase.json
  firestore.rules
  docs/
    ESPECIFICACAO.md
    PROMPT_CODEX.md
```

## Observação

Enquanto o Firebase não estiver configurado, o sistema entra automaticamente em modo demonstração com localStorage. Isso permite testar o fluxo completo, gerar PDFs e validar o MVP sem depender de infraestrutura.
