# Checklist da release ASP.NET 1.0.0

## Identificação e aprovação

- [ ] Registrar commit, checksum, responsável, data e janela de deploy.
- [ ] Confirmar ausência de P0/P1 e aprovação humana da homologação.
- [ ] Confirmar que somente o artefato ASP.NET atual será instalado.

## Validação técnica

- [ ] `dotnet restore OrcaFacil.sln`
- [ ] `dotnet build src/OrcaFacil.Web/OrcaFacil.Web.csproj --configuration Debug`
- [ ] `dotnet build OrcaFacil.sln --configuration Debug`
- [ ] `dotnet build OrcaFacil.sln --configuration Release`
- [ ] `dotnet test OrcaFacil.sln --configuration Debug`
- [ ] `dotnet test OrcaFacil.sln --configuration Release`
- [ ] `dotnet publish src/OrcaFacil.Web/OrcaFacil.Web.csproj -c Release -o artifacts/publish/orcafacil-web`
- [ ] `npm ci` e todos os gates de `npm run check:release-final` aprovados.

## Banco e configuração

- [ ] Backup datado, não vazio e restaurado com sucesso em destino isolado.
- [ ] Instalação limpa e upgrade aditivo aprovados com registros existentes preservados.
- [ ] Seeds idempotentes não duplicam nem recriam usuários.
- [ ] `PublicBaseUrl`, HTTPS, HSTS, cookie seguro e chaves persistentes de Data Protection configurados.
- [ ] Segredos fornecidos apenas por mecanismo seguro do ambiente.

## Homologação

- [ ] Perfis público, novo usuário, comercial, externo, operacional, financeiro, administrador e SuperAdmin aprovados.
- [ ] Login, onboarding, orçamento/proposta, OS, pagamento/recibo e contratos aprovados.
- [ ] Isolamento por `AccountId`, antiforgery, autorização Admin e sanitização de logs aprovados.
- [ ] Console/Network sem erros inesperados e matriz de 320 a 1440 px sem quebra.

## Deploy e pós-deploy

- [ ] Artefato anterior e procedimento de `docs/ROLLBACK-V1.md` disponíveis.
- [ ] Colocar em manutenção, instalar pacote, aplicar patch aditivo e iniciar aplicação.
- [ ] Validar `/health`, login, proposta anônima, fluxo principal, logs, auditoria e EmailOutbox.
- [ ] Iniciar rotina de `docs/OPERACAO-ASSISTIDA-V1.md` e registrar decisão final.
