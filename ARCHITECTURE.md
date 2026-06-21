# Arquitetura do OrçaFácil

## Visão geral

O OrçaFácil é um SaaS freemium front-end first para geração de orçamentos e recibos em PDF. A aplicação principal fica em `public/` e roda como site estático em Firebase Hosting, IIS, Apache, Nginx ou pelo servidor local Node/Fastify.

## Objetivo do projeto

Permitir que autônomos, MEIs e pequenos prestadores criem documentos profissionais pelo navegador/celular, mantendo baixo custo operacional e arquitetura simples, sem build obrigatório e sem backend complexo nesta etapa.

## Módulos principais

- **Landing e aplicação:** `public/index.html`, `public/css/app.css` e módulos ES em `public/js/`.
- **Autenticação:** Firebase Authentication quando conectado; modo demonstração em `localStorage` quando solicitado.
- **Persistência:** Cloud Firestore para usuários reais; `localStorage` para demonstração.
- **PDF:** `jsPDF` e `autoTable` em `public/js/pdf.js`.
- **Aprovação pública:** `public/aprovar.html` e `public/js/public-approval.js`.
- **Servidor local opcional:** `server.js`, servindo a pasta `public/` na porta 8095.

## Estrutura de pastas planejada

```text
/public
  /css
    app.css
  /js
    /core              Configuração, constantes, eventos e bootstrap leve.
    /domain            Classes simples do domínio.
    /services          Casos de uso e orquestração.
    /repositories      Isolamento de Firebase/localStorage.
    /ui                Componentes de tela migrados gradualmente.
    /utils             Utilitários pequenos e puros.
    app.js             Aplicação atual mantida por compatibilidade.
    firebase-config.js Configuração Web pública do Firebase.
  index.html
  aprovar.html
```

## Responsabilidade de cada camada

- **Domain:** representa conceitos como `DocumentModel`, `DocumentItem`, `IssuerProfile`, `UserAccount`, `PlanModel` e `PublicQuoteModel`. Não deve acessar DOM, Firebase ou `localStorage`.
- **Services:** expõe operações de negócio como salvar documento, autenticar, gerar PDF, exportar e gerenciar plano.
- **Repositories:** encapsula a origem dos dados para evitar Firestore espalhado na UI.
- **UI:** controla DOM e eventos de tela. A migração será feita em fases para não quebrar o MVP.
- **Utils:** funções puras para moeda, data, validações, texto, IDs e erros.
- **Core:** configurações estáticas, constantes e eventos internos.

## Fluxo de criação de documento

1. A UI coleta dados do formulário.
2. `DocumentModel` normaliza itens, datas, status e totais.
3. `NumberingService` calcula o próximo número `ORC-000001` ou `REC-000001`.
4. `DocumentService` salva o documento.
5. `DocumentRepository` persiste em Firestore ou `localStorage`, conforme o modo ativo.
6. O histórico recarrega e exibe o documento salvo.

## Fluxo de geração de PDF

1. A UI valida o documento atual.
2. O documento é salvo para garantir numeração e histórico.
3. `PdfService` delega para o gerador existente em `public/js/pdf.js`.
4. O PDF usa dados do emitente e plano atual.
5. No plano Free, a marca OrçaFácil é exibida; no Pro, a marca é removida.

## Fluxo de autenticação

1. A aplicação inicializa o adapter atual em `public/js/services.js`.
2. Firebase Authentication é usado quando disponível e quando o usuário não ativa demonstração.
3. `AuthService` define a interface desejada: `login`, `register`, `logout`, `onAuthChanged` e `getCurrentUser`.
4. Após login, o perfil e documentos do usuário são carregados.

## Fluxo Firebase/localStorage

- **Firebase:** dados ficam em `users/{uid}`, `users/{uid}/settings/profile`, `users/{uid}/documents/{documentId}` e `publicQuotes/{token}`.
- **Demonstração:** dados equivalentes ficam no `localStorage` do navegador.
- A UI deve evoluir para usar services/repositories, sem chamadas diretas ao Firestore.

## Fluxo Free/Pro

1. O plano é lido do documento do usuário/perfil.
2. `PlanService` centraliza `isFree`, `isPro` e link de upgrade via WhatsApp.
3. Recursos visuais e PDF reagem ao plano.
4. A ativação Pro continua manual no Firestore nesta etapa.

## Futuro fluxo de aprovação pública

1. Um orçamento salvo gera token público.
2. `PublicQuoteModel` representa o índice público.
3. `PublicApprovalService` carrega o orçamento por token.
4. O cliente aprova ou recusa sem login.
5. A decisão atualiza documento e histórico.

## Convenções de nomes

- Arquivos de domínio: `*.model.js`.
- Services: `*.service.js`.
- Repositories: `*.repository.js`.
- UI: `*.ui.js`.
- Utilitários: nomes curtos por responsabilidade (`currency.js`, `date.js`).
- Módulos sempre em ES Modules, sem build obrigatório.

## Como evoluir sem quebrar

- Migrar em fases: primeiro criar wrappers e classes, depois mover regras de negócio e por último quebrar `app.js` em telas menores.
- Manter imports relativos (`./js/app.js`, `./css/app.css`) para suportar subpastas.
- Evitar dependência de raiz `/`.
- Não remover modo demonstração, login Firebase, histórico, PDF ou Free/Pro durante refatorações.
- Preferir alterações pequenas e testáveis.

## Execução estática e ambientes

O OrçaFácil é uma aplicação front-end estática. O servidor Node/Fastify em `server.js` é apenas uma conveniência local para desenvolvimento, demonstração e validação rápida na porta `8095`; ele não é um backend de negócio obrigatório.

### Ambientes suportados

- **Firebase Hosting:** publicação oficial recomendada para produção estática. A pasta publicada deve ser `public/`.
- **IIS:** pode servir diretamente `public/` como site/aplicação. Também pode apontar para a raiz do projeto; nesse caso, o `index.html` da raiz redireciona para `public/index.html` quando a origem é HTTP/HTTPS.
- **Apache/Nginx/hospedagens estáticas:** devem servir a pasta `public/` ou preservar os caminhos relativos da raiz até `public/`.
- **Node/Fastify local:** serve `public/` na raiz (`http://localhost:8095`) e também expõe `/public/` para validar cenários em que o site está abaixo de uma subpasta.

### Por que `file://` não é suportado

A aplicação usa ES Modules, imports entre arquivos, Firebase SDK modular por CDN, jsPDF/autoTable e armazenamento do navegador. Ao abrir `public/index.html` diretamente pelo sistema de arquivos, o navegador usa origem `null` e bloqueia imports locais por segurança, normalmente exibindo erro de CORS. Por isso, `file://` não é um ambiente suportado.

O `index.html` da raiz não importa `app.js`; ele apenas detecta o protocolo. Em `file://`, mostra uma mensagem amigável com instruções. Em HTTP/HTTPS, redireciona para `./public/index.html`. O `public/index.html` também possui uma verificação mínima antes de inserir dinamicamente o módulo principal, evitando inicializar Firebase e módulos adicionais quando o protocolo é `file:`.

### Caminhos relativos

Mantenha CSS, JavaScript e páginas internas com caminhos relativos, como `./css/app.css`, `./js/app.js`, `./diagnostico.html` e `./aprovar.html`. Evite URLs iniciadas por `/`, pois elas quebram quando o OrçaFácil é publicado em subpastas como `https://dominio.com.br/orcafacil` ou `http://localhost/OrcaFacil`.

### Sem dependência de backend próprio

Regras de negócio do produto continuam no front-end modular e na camada Firebase/Firestore. O modo demonstração usa `localStorage`. Não introduza dependência obrigatória de build, SSR, API própria, Cloud Functions novas ou frameworks como React/Vue/Angular para manter a publicação compatível com hospedagem estática simples.

## Camada de observabilidade e suporte

### Logger e auditoria
`logger.service.js` é a camada central para logs de front-end. Ele cria modelos compatíveis com `system-log.model.js`, persiste em Firestore ou `localStorage` no modo demo e separa logs operacionais (`systemLogs`), eventos (`systemEvents`), erros (`systemErrors`) e auditoria (`auditLogs`). A auditoria cobre login, cadastro, perfil, documentos, PDF, exportações, links públicos, WhatsApp, Telegram e chatbot.

### Fluxo de erro
1. A ação crítica inicia com `try/catch`.
2. Em sucesso, registra evento `*_SUCCESS` ou auditoria.
3. Em falha, mostra mensagem amigável ao usuário.
4. O erro técnico, stack, URL, userAgent e ambiente vão para `systemErrors`.
5. O `super_admin` consulta e resolve em **Admin Geral > Erros/Bugs**.

### Monitoramento administrativo
`AdminUI` expõe abas para usuários, logs, eventos, bugs, auditoria, Telegram, configurações e saúde. `AdminService` centraliza consultas e escrita em `adminSettings/contact`, `adminSettings/chatbot` e `adminSettings/logging`.

### Configurações administrativas
- `adminSettings/contact`: WhatsApp/e-mail público da MNSOFT.
- `adminSettings/chatbot`: modo local, nome, exibição e escalação.
- `adminSettings/logging`: nível mínimo e limite de logs por sessão.

### Fluxo de suporte via WhatsApp
Usuário clica em WhatsApp no chatbot ou assinatura. O sistema monta `https://wa.me/{whatsappNumber}?text={mensagem}` com dados públicos da MNSOFT. Nenhuma credencial é usada no front-end.

### Chatbot local
A UI flutuante (`chatbot.ui.js`) chama `ChatbotService`, que procura respostas em `chatbot-knowledge-base.js`, aplica filtros de segurança, registra eventos/auditoria e salva histórico local no navegador. O modo local funciona em Firebase Hosting, IIS/static, Node local e demonstração.

### Chatbot IA futuro
Uma implementação futura deve usar Cloud Function callable `askChatbot`; tokens ficam em variáveis seguras, nunca no front-end. A função deve validar autenticação, escopo OrçaFácil, filtros LGPD, limite de tamanho e logging.
