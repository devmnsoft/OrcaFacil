#!/usr/bin/env bash
set -e
cd "$(dirname "$0")/.."
echo "========================================"
echo "Iniciando OrçaFácil"
echo "========================================"
if ! command -v node >/dev/null 2>&1; then
  echo "Node.js não encontrado. Instale Node.js 18 ou superior."
  exit 1
fi
NODE_MAJOR="$(node -p "process.versions.node.split('.')[0]")"
if [ "$NODE_MAJOR" -lt 18 ]; then
  echo "Node.js 18 ou superior é necessário. Versão atual: $(node -v)"
  exit 1
fi
if [ ! -d "node_modules" ]; then
  echo "Instalando dependências..."
  npm install
fi
echo "Iniciando servidor..."
npm start
