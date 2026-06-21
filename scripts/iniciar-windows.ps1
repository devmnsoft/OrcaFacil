$ErrorActionPreference = "Stop"
Write-Host "========================================"
Write-Host "Iniciando OrçaFácil"
Write-Host "========================================"
$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) { Write-Host "Node.js não encontrado. Instale Node.js 18 ou superior."; Read-Host "Pressione Enter"; exit 1 }
$major = [int](& node -p "process.versions.node.split('.')[0]")
if ($major -lt 18) { Write-Host "Node.js 18 ou superior é necessário. Versão atual: $(& node -v)"; Read-Host "Pressione Enter"; exit 1 }
if (-not (Test-Path "node_modules")) { Write-Host "Instalando dependências..."; npm install }
Write-Host "Iniciando servidor..."
npm start
Read-Host "Pressione Enter para sair"
