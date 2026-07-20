@echo off
setlocal
dotnet restore OrcaFacil.sln
dotnet build OrcaFacil.sln -c Release --no-restore
dotnet test OrcaFacil.sln -c Release --no-build
dotnet publish src/OrcaFacil.Web/OrcaFacil.Web.csproj -c Release -o publish/iis
endlocal
