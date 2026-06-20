@echo off
cd /d %~dp0
if not exist node_modules (
  echo Instalando dependencias...
  npm install
)
set PORT=8095
npm start
pause
