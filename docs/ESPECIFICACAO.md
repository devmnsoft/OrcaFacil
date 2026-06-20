# Especificação funcional — OrçaFácil MVP

## Visão do produto

O OrçaFácil é um SaaS simples para autônomos, MEIs e pequenas empresas criarem orçamentos e recibos profissionais em PDF, com numeração, dados do emitente, dados do cliente, itens, totais, observações e histórico.

## Objetivo do MVP

Entregar valor imediato em poucos segundos: preencher, gerar PDF e enviar ao cliente. O sistema não emite nota fiscal e não substitui sistemas fiscais. O foco é orçamento e recibo comercial.

## Perfis

- Usuário gratuito: gera PDFs com marca “Gerado com OrçaFácil”.
- Usuário Pro: remove a marca e libera experiência profissional.

## Módulos

### 1. Autenticação

- Cadastro por e-mail e senha.
- Login por e-mail e senha.
- Logout.
- Modo demonstração com localStorage quando Firebase não está configurado.

### 2. Emitente

Campos:

- Nome/razão social.
- CPF/CNPJ.
- Telefone/WhatsApp.
- E-mail.
- Cidade/UF.
- Chave Pix.
- Logo em base64.
- Plano: free/pro.

### 3. Documento

Tipos:

- Orçamento.
- Recibo.

Campos:

- Número sequencial.
- Cliente.
- Documento do cliente.
- Contato do cliente.
- Cidade.
- Data.
- Validade, somente para orçamento.
- Observações.
- Itens com descrição, quantidade, valor unitário, desconto e total.

### 4. PDF

O PDF deve conter:

- Logo do emitente, quando cadastrada.
- Dados do emitente.
- Título do documento.
- Número e data.
- Dados do cliente.
- Tabela de itens.
- Total geral.
- Valor por extenso para recibo.
- Observações.
- Rodapé freemium quando plano for gratuito.

### 5. Histórico

- Listar documentos salvos.
- Filtrar por tipo/status/texto.
- Abrir para edição.
- Duplicar documento.
- Reemitir PDF.
- Excluir documento.

## Regras principais

- Todo documento deve ter pelo menos um item válido.
- Numeração deve ser sequencial por usuário.
- O total é calculado automaticamente.
- O plano gratuito mantém marca do produto no PDF.
- O plano Pro remove marca.
- Recibo deve exibir valor por extenso.

## Stack

- Front-end: HTML, Bootstrap 5 e JavaScript ES Modules.
- PDF: jsPDF e jspdf-autotable.
- Autenticação e banco: Firebase Authentication e Firestore.
- Hospedagem: Firebase Hosting.
- Servidor local: Node.js + Fastify na porta 8095.

## Roadmap sugerido

1. Publicar MVP no Firebase Hosting.
2. Testar com prestadores reais.
3. Melhorar layout do PDF conforme feedback.
4. Implementar cobrança recorrente.
5. Criar página comercial com SEO.
6. Adicionar conversão de orçamento aprovado em recibo.
7. Envio direto por WhatsApp/e-mail.
