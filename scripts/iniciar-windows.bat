@echo off
title OrcaFacil - Servidor Local
cls
echo ========================================
echo Iniciando OrcaFacil
echo ========================================

where node >nul 2>nul
if %errorlevel% neq 0 (
  echo Node.js nao encontrado.
  echo Instale o Node.js 18 ou superior.
  pause
  exit /b 1
)

for /f "tokens=1 delims=." %%v in ('node -p "process.versions.node"') do set NODE_MAJOR=%%v
if %NODE_MAJOR% LSS 18 (
  echo Node.js 18 ou superior e necessario.
  node -v
  pause
  exit /b 1
)

if not exist node_modules (
  echo Instalando dependencias...
  npm install
)

echo Iniciando servidor...
npm start
pause
