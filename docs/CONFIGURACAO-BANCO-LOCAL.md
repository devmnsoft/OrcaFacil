# Configuração local do PostgreSQL

A resolução usa, nesta ordem: `ORCAFACIL_DATABASE_URL`, `ConnectionStrings__DefaultConnection` e os provedores normais do ASP.NET (user secrets, `appsettings.Development.json` e `appsettings.json`). Nenhuma sentinela é usada quando a configuração falta.

Não versione senhas. Configure cada host com user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=SUA_SENHA;Pooling=true;Timeout=15" --project src/OrcaFacil.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=SUA_SENHA;Pooling=true;Timeout=15" --project src/OrcaFacil.Api
```

Os arquivos `appsettings.Development.example.json` documentam o formato. Porta 1, banco `unavailable`, senha vazia/placeholder, host, banco ou usuário ausentes são rejeitados. Em Production a aplicação não inicia com configuração inválida.
