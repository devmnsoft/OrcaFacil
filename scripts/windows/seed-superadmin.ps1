param([string]$ConnectionString)
. "$PSScriptRoot/common.ps1"
if (-not $env:ORCAFACIL_SUPERADMIN_EMAIL -or -not $env:ORCAFACIL_SUPERADMIN_PASSWORD) { throw 'Defina ORCAFACIL_SUPERADMIN_EMAIL e ORCAFACIL_SUPERADMIN_PASSWORD. A senha não será gravada no repositório.' }
Invoke-PsqlFile (Get-DatabaseUrl $ConnectionString) (Join-Path $RepoRoot 'database/seed-superadmin.sql')
