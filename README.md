# OrçaFácil

OrçaFácil é um SaaS freemium simples para autônomos, MEIs e pequenas empresas criarem **orçamentos e recibos profissionais em PDF** direto pelo navegador ou celular.

## Tecnologias

- HTML5, CSS3 e JavaScript puro
- Bootstrap 5 e Bootstrap Icons
- jsPDF e jsPDF AutoTable
- Firebase Auth, Firestore e Hosting
- Node.js com Fastify para servidor local
- LocalStorage para modo demonstração sem Firebase

## Como rodar localmente

```bash
npm install
npm start
```

Acesse: <http://localhost:8095>

O servidor local usa a porta **8095**.

## Modo demonstração

Se `public/js/firebase-config.js` não estiver configurado, o app funciona em modo demonstração local. Nesse modo, perfil, plano, documentos, rascunho e histórico são salvos no `localStorage` do navegador.

## Configurar Firebase

1. Crie um projeto no Firebase Console.
2. Ative Authentication com provedor de e-mail/senha.
3. Crie um banco Firestore.
4. Copie as credenciais web para `public/js/firebase-config.js`.
5. Publique as regras de segurança de `firestore.rules`.

Estrutura usada no Firestore:

```text
users/{uid}
users/{uid}/documents/{documentId}
```

Cada usuário acessa apenas o próprio documento, conforme `firestore.rules`.

## Publicar no Firebase Hosting

```bash
firebase login
firebase init hosting
firebase deploy
```

Configure a pasta pública como `public` e mantenha o app como aplicação estática.

## Alternar usuário Free/Pro

- No app, entre em **Emitente**.
- Altere o campo **Plano** para `Free` ou `Pro`.
- Salve o emitente.

Com o plano `free`, o PDF exibe a marca OrçaFácil. Com o plano `pro`, a marca é removida.

## Estrutura de pastas

```text
server.js                 Servidor local Fastify na porta 8095
firestore.rules           Regras de segurança do Firestore
public/index.html         Landing page e interface principal
public/css/app.css        Estilos visuais e responsividade
public/js/app.js          UI, formulários, histórico, planos e fluxo do app
public/js/pdf.js          Geração dos PDFs com jsPDF
public/js/services.js     Camada LocalStorage/Firebase
public/js/utils.js        Funções utilitárias, moeda, datas e cálculos
public/js/firebase-config.js Configuração do Firebase
```

## Funcionalidades atuais

- Landing page comercial com hero, como funciona, público-alvo, planos e CTA.
- Login/cadastro quando Firebase está configurado.
- Modo demonstração local sem Firebase.
- Cadastro de dados do emitente, plano e logo.
- Orçamentos e recibos com itens, quantidade, valor, desconto e total automático.
- Máscara visual simples para moeda.
- Rascunho local para evitar perda de dados ao navegar.
- Histórico com filtro por tipo, busca por cliente/número/observação, total e data.
- Ações no histórico: abrir, editar, duplicar, gerar PDF e excluir com confirmação.
- Tela “Minha assinatura” com incentivo ao Pro.
- PDFs com cabeçalho profissional, dados organizados, tabela limpa, total destacado, observações, rodapé, recibo com valor por extenso e assinatura.

## Roadmap futuro

- Pagamento recorrente.
- Envio automático por WhatsApp.
- Envio por e-mail.
- Conversão de orçamento aprovado em recibo.
- Upload real de logo no Firebase Storage.
- Dashboard de faturamento.
- Múltiplos usuários por conta.
- Prefixo de numeração por ano.
- Página pública para cliente aprovar orçamento.
