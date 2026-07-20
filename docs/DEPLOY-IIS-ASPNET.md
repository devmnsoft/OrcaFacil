# Deploy IIS ASP.NET Core

1. Instale o .NET Hosting Bundle compatível com ASP.NET Core 10.
2. Execute `publish-iis.bat`.
3. Crie site no IIS apontando para `publish/iis`.
4. Configure Application Pool como **No Managed Code**.
5. Defina `ASPNETCORE_ENVIRONMENT=Production` e a connection string via variável de ambiente/secret.
6. Conceda permissão de leitura/gravação apenas para pasta de logs e uploads.
7. Para 500.19, valide `web.config`. Para 500.30, consulte Event Viewer e logs da aplicação.
