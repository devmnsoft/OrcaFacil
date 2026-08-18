# Deploy do OrçaFácil ASP.NET Core no Windows/IIS

Este procedimento publica **somente** `src/OrcaFacil.Web` (Razor Pages/.NET/PostgreSQL). O site estático legado não faz parte da RC V1.

## Pré-requisitos

1. Windows Server com IIS e o **ASP.NET Core Hosting Bundle** compatível com o `TargetFramework` do projeto.
2. PostgreSQL e ferramentas `psql`, `pg_dump` e `pg_restore` no `PATH`.
3. Identidade do App Pool com leitura na publicação e modificação apenas em `C:\ProgramData\OrcaFacil\{keys,logs,uploads,backups}`.
4. Certificado e binding HTTPS no IIS. Não exponha a aplicação diretamente em HTTP na produção.

## Publicar e instalar

```powershell
.\scripts\windows\check-environment.ps1 -Iis
.\scripts\windows\update-database.ps1 -ConnectionString $env:ConnectionStrings__DefaultConnection
.\scripts\windows\publish-release.ps1
.\scripts\windows\install-iis.ps1 -SiteName OrcaFacil -PhysicalPath C:\inetpub\OrcaFacil -Port 8080
Copy-Item .\artifacts\publish\orcafacil-web\* C:\inetpub\OrcaFacil -Recurse -Force
```

Configure o App Pool como **No Managed Code**, com uma identidade dedicada. O `web.config` usa `AspNetCoreModuleV2`, `hostingModel="inprocess"` e inicia `OrcaFacil.Web.dll`; o app não depende do Visual Studio.

## Variáveis obrigatórias

Cadastre no ambiente do serviço (ou transforme em `<environmentVariable>` no IIS usando configuração protegida):

- `ConnectionStrings__DefaultConnection`;
- os três valores `Security__*Pepper` com valores aleatórios independentes;
- `Application__PublicBaseUrl` com URL HTTPS final;
- `DataProtection__KeysPath`, `Uploads__Path` e `SystemHealth__LogsPath` fora de `wwwroot`;
- credenciais `Email__*` somente quando SMTP estiver habilitado.

Nunca grave connection string, senha ou pepper no `web.config` ou `appsettings.Production.json`. Conceda à identidade do App Pool acesso persistente à pasta de chaves antes do primeiro login.

## Atualização, observabilidade e rollback

1. Gere backup: `.\scripts\windows\backup-db.ps1`.
2. Pare o App Pool, guarde a pasta publicada anterior e aplique o SQL idempotente.
3. Copie a nova publicação e inicie o App Pool.
4. Valide `/health/live`, `/health/ready`, login e `/Diagnostico` com SuperAdmin.
5. Em falha, restaure os binários anteriores. Banco só deve ser restaurado por decisão operacional explícita com `restore-db.ps1 -ConfirmRestore -Confirm`.

O stdout do módulo fica desabilitado normalmente. Em diagnóstico de inicialização, habilite-o temporariamente no `web.config`, assegure permissão na pasta de logs e desabilite-o após coletar o erro. Os logs Serilog têm rotação diária e retenção de 30 arquivos.
