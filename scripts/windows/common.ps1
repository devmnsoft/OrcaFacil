Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
function Require-Command([string]$Name) { if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { throw "Comando obrigatório não encontrado: $Name" } }
function Get-DatabaseUrl([string]$Value) { if ($Value) { return $Value }; if ($env:ORCAFACIL_DATABASE_URL) { return $env:ORCAFACIL_DATABASE_URL }; if ($env:ConnectionStrings__DefaultConnection) { return $env:ConnectionStrings__DefaultConnection }; throw 'Informe -ConnectionString ou ORCAFACIL_DATABASE_URL.' }
function Invoke-PsqlFile([string]$ConnectionString,[string]$File) { Require-Command 'psql'; if (-not (Test-Path $File -PathType Leaf)) { throw "SQL não encontrado: $File" }; & psql $ConnectionString --set ON_ERROR_STOP=1 --file $File; if ($LASTEXITCODE -ne 0) { throw "psql terminou com código $LASTEXITCODE" } }
