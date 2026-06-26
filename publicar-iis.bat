@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ========================================
echo OrcaFacil - Publicador IIS
echo ========================================
echo.

echo 1. Verificando Node.js...
where node >nul 2>nul
if errorlevel 1 (
  echo ERRO: Node.js nao encontrado. Instale Node.js 18 ou superior.
  pause
  exit /b 1
)
for /f "tokens=1 delims=.v" %%A in ('node -v') do set NODE_MAJOR=%%A
if %NODE_MAJOR% LSS 18 (
  echo ERRO: Node.js 18 ou superior e necessario. Versao atual:
  node -v
  pause
  exit /b 1
)

echo 2. Verificando arquivos do projeto...
if not exist package.json (
  echo ERRO: execute este publicador na raiz do projeto OrcaFacil.
  pause
  exit /b 1
)
if not exist scripts\publish-iis.mjs (
  echo ERRO: scripts\publish-iis.mjs nao encontrado.
  pause
  exit /b 1
)

echo 3. Verificando npm...
where npm >nul 2>nul
if errorlevel 1 (
  echo ERRO: npm nao encontrado. Reinstale o Node.js 18 ou superior incluindo npm.
  pause
  exit /b 1
)

echo 4. Instalando dependencias, se necessario...
if not exist node_modules (
  call npm install
  if errorlevel 1 (
    echo ERRO: npm install falhou.
    pause
    exit /b 1
  )
) else (
  echo node_modules encontrado. Pulando npm install.
)

echo 5. Limpando pasta dist...
if exist dist rmdir /s /q dist

echo 6. Gerando build de publicacao...
call npm run publish:iis
if errorlevel 1 (
  echo ERRO: build/publicacao falhou.
  pause
  exit /b 1
)

echo 7. Validando arquivos obrigatorios...
if not exist dist (
  echo ERRO: pasta dist nao foi criada.
  pause
  exit /b 1
)
if not exist dist\web.config (
  echo ERRO: dist\web.config nao foi criado.
  pause
  exit /b 1
)
if not exist dist\instalacao.html (
  echo ERRO: dist\instalacao.html nao foi criado.
  pause
  exit /b 1
)
if not exist dist\diagnostico.html (
  echo ERRO: dist\diagnostico.html nao foi criado.
  pause
  exit /b 1
)
if not exist dist\version.json (
  echo ERRO: dist\version.json nao foi criado.
  pause
  exit /b 1
)

echo 8. Gerando pacote ZIP, se disponivel...
where powershell >nul 2>nul
if errorlevel 1 (
  echo PowerShell nao encontrado. Pasta dist gerada com sucesso. Compacte manualmente se desejar.
) else (
  if not exist publish mkdir publish
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path 'dist\*' -DestinationPath 'publish\orcafacil-iis-dist.zip' -Force" >nul 2>nul
  if errorlevel 1 (
    echo Nao foi possivel gerar ZIP automaticamente. Compacte manualmente se desejar.
  ) else (
    echo ZIP gerado em publish\orcafacil-iis-dist.zip
  )
)

echo 9. Publicacao finalizada.
echo.
echo Publicacao finalizada com sucesso.
echo Copie o conteudo da pasta dist para o IIS.
echo Depois acesse /instalacao.html e /diagnostico.html no dominio publicado.
start "" explorer "%cd%\dist"
pause
endlocal
