param([Parameter(Mandatory=$true)][string]$ConnectionString,
      [Parameter(Mandatory=$true)][string]$DemoEmail)
$ErrorActionPreference = 'Stop'
if ($env:ASPNETCORE_ENVIRONMENT -ne 'Development') { throw 'Seed Demo permitido somente em Development.' }
if ($env:DEMO_SEED_ENABLED -ne 'true') { throw 'Defina DEMO_SEED_ENABLED=true explicitamente.' }
$env:PGOPTIONS = "-c orcafacil.demo_seed_enabled=true -c orcafacil.demo_email=$DemoEmail"
psql $ConnectionString -v ON_ERROR_STOP=1 -f (Join-Path $PSScriptRoot '../database/seed_demo_release_candidate_v64.sql')
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o seed Demo V6.4 ($LASTEXITCODE)." }
