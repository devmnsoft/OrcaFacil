# Publicação IIS ASP.NET Core

1. Instale o .NET 10 SDK/Runtime e o ASP.NET Core Hosting Bundle no servidor.
2. Configure PostgreSQL e a connection string `DefaultConnection`.
3. Execute `dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web`.
4. Execute `publish-iis.bat`.
5. Aponte o site IIS para `publish/iis` e use Application Pool `No Managed Code`.
