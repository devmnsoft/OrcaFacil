# Publicação IIS do OrçaFácil

Este guia explica como gerar e publicar a pasta `dist/` do OrçaFácil em um IIS estático. Node.js é usado somente para preparar os arquivos; depois da publicação, o IIS serve apenas HTML, CSS, JavaScript e assets estáticos.

## Requisitos

- Windows com acesso ao projeto.
- Node.js 18 ou superior instalado.
- npm disponível no terminal.
- IIS habilitado com conteúdo estático.
- Domínio final cadastrado no Firebase Authentication em **Authorized domains**.

## Gerar a publicação com 1 clique

1. Abra a raiz do projeto no Windows Explorer.
2. Dê duplo clique em `publicar-iis.bat`.
3. Aguarde as etapas de verificação, instalação de dependências, geração de `dist/`, validação e ZIP opcional.
4. Ao final, a pasta `dist` será aberta automaticamente.

Também é possível executar pelo terminal:

```bash
npm run publish:iis
```

## Onde fica a pasta final

A publicação fica em:

```text
dist/
```

Arquivos principais gerados:

- `dist/index.html`
- `dist/web.config`
- `dist/web.rewrite.config` (opcional, somente com URL Rewrite instalado)
- `dist/version.json`
- `dist/LEIA-ME-PUBLICACAO.txt`
- `dist/instalacao.html`
- `dist/diagnostico.html`
- `dist/css/app.css`
- `dist/js/app.js`

## Configurar no IIS

1. Copie a pasta `dist` para:

   ```text
   C:\inetpub\wwwroot\orcafacil
   ```

2. No IIS Manager, crie um site ou aplicação apontando para:

   ```text
   C:\inetpub\wwwroot\orcafacil
   ```

3. Configure o documento padrão como:

   ```text
   index.html
   ```

4. Acesse:

   ```text
   http://localhost/orcafacil
   ```

Não abra `index.html` por `file://`; o sistema precisa de HTTP ou HTTPS para ES Modules, Firebase Auth, Firestore e geração de PDF.

## web.config e URL Rewrite

O `dist/web.config` principal não depende de ASP.NET nem do módulo URL Rewrite. Ele configura documento padrão, MIME types para JS/MJS/JSON/CSS/SVG/WEBP e headers básicos de segurança.

O arquivo `dist/web.rewrite.config` é uma alternativa para fallback de SPA. Use-o somente se o IIS tiver o módulo **URL Rewrite** instalado. Para usar, renomeie o `web.config` atual para backup e copie o conteúdo de `web.rewrite.config` para `web.config`.

## Configurar domínio e Firebase

1. Publique o site no domínio final com HTTPS.
2. Acesse o Firebase Console.
3. Vá em **Authentication > Settings > Authorized domains**.
4. Adicione o domínio usado pelo IIS, por exemplo:
   - `localhost` para testes locais.
   - `orcafacil.suaempresa.com.br` para produção.

## Testar localmente a dist

Depois de gerar a pasta:

```bash
npm run serve:dist
```

Acesse:

```text
http://localhost:8095
```

Valide:

- `/instalacao.html` mostra o assistente e o checklist pós-publicação.
- `/diagnostico.html` mostra protocolo, domínio, Firebase, App Check, `version.json` e localStorage.
- Tela inicial carrega sem erro de MIME type.
- Login Firebase funciona.
- Modo demonstração funciona.
- Geração de PDF funciona.
- Histórico funciona.
- Console do navegador não mostra erro de import/module.

## Atualizar uma nova versão

1. Gere uma nova `dist` executando `publicar-iis.bat` ou `npm run publish:iis`.
2. Faça backup da pasta atual do IIS.
3. Substitua os arquivos antigos pelos novos arquivos de `dist`.
4. Limpe cache do navegador se necessário.
5. Teste login, demonstração, PDF e histórico.

## Rollback

1. Antes de substituir a pasta atual no IIS, renomeie a pasta antiga para `orcafacil_backup`.
2. Copie a nova `dist` para o local de produção.
3. Se der erro, remova a nova pasta.
4. Restaure `orcafacil_backup` para o nome original.
5. Reinicie o site/aplicação no IIS se necessário.

## Segurança da publicação

O publicador bloqueia arquivos sensíveis e valida que `dist` não contenha `.env`, `functions/.env`, `node_modules`, `scripts`, `tests`, `.git` ou termos como `private_key`, `serviceAccount`, `TELEGRAM_BOT_TOKEN`, `MERCADO_PAGO_ACCESS_TOKEN` e `OPENAI_API_KEY`.


## Checklist pós-publicação

Depois de copiar a `dist/` para o IIS, abra `https://SEU_DOMINIO/instalacao.html` e confirme:

- O site está em HTTP/HTTPS, nunca em `file://`.
- O domínio final aparece em **Firebase Authentication > Settings > Authorized domains**.
- `https://SEU_DOMINIO/diagnostico.html` não mostra erros críticos.
- Login Firebase, logout e recuperação de sessão funcionam.
- Modo demonstração salva dados no `localStorage`.
- Orçamento, recibo, PDF e histórico foram testados.
- `https://SEU_DOMINIO/version.json` abre no navegador.
- Console do navegador não mostra erro de MIME type, import ES Module ou Firebase Auth domain.

## Erros comuns

- **`file://`**: rode `npm start`, `npm run serve:dist` ou publique em servidor HTTP/HTTPS.
- **Login bloqueado por domínio**: cadastre o domínio exato no Firebase Authorized domains.
- **JS/MJS baixando ou com MIME inválido**: confirme o `web.config` gerado e o recurso Conteúdo Estático no IIS.
- **Rotas internas retornando 404**: use `web.rewrite.config` como `web.config` somente se o módulo IIS URL Rewrite estiver instalado.
