param([string]$ConnectionString,[string]$OutputDirectory = 'backups')
. "$PSScriptRoot/common.ps1"; Require-Command 'pg_dump'
$db = Get-DatabaseUrl $ConnectionString; $dir = Join-Path $RepoRoot $OutputDirectory; New-Item $dir -ItemType Directory -Force | Out-Null
$file = Join-Path $dir ("orcafacil-{0}.dump" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
& pg_dump --dbname=$db --format=custom --no-owner --no-acl --file=$file
if ($LASTEXITCODE -ne 0) { Remove-Item $file -Force -ErrorAction SilentlyContinue; throw 'Backup não foi concluído.' }
Write-Host "Backup validado: $file" -ForegroundColor Green
