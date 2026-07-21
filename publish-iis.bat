@echo off
setlocal
set SOLUTION=OrcaFacil.sln
set PUBLISH_DIR=publish\iis

echo [1/4] Restaurando pacotes...
dotnet restore %SOLUTION%
if errorlevel 1 goto :erro

echo [2/4] Compilando...
dotnet build %SOLUTION% --configuration Release --no-restore
if errorlevel 1 goto :erro

echo [3/4] Executando testes...
dotnet test %SOLUTION% --configuration Release --no-build
if errorlevel 1 goto :erro

echo [4/4] Publicando OrcaFacil.Web para IIS...
dotnet publish src\OrcaFacil.Web\OrcaFacil.Web.csproj --configuration Release --output %PUBLISH_DIR% --no-build
if errorlevel 1 goto :erro

echo.
echo Publicacao concluida em %PUBLISH_DIR%.
echo Proximos passos: configure o Application Pool como No Managed Code, instale o Hosting Bundle do ASP.NET Core, ajuste appsettings.Production.json e a connection string PostgreSQL, aplique migrations e aponte o site IIS para a pasta publicada.
pause
exit /b 0

:erro
echo.
echo ERRO: publicacao interrompida. Corrija a etapa acima antes de copiar arquivos para o IIS.
pause
exit /b 1
