# Prepara um PostgreSQL local descartável para homologação.
# A connection string vem só de ORCAFACIL_HOMOLOG_DATABASE_URL.
# O banco precisa ser localhost e o nome precisa terminar em _homolog ou _test.
param([switch]$WhatIf)
$ErrorActionPreference = 'Stop'
$url = $env:ORCAFACIL_HOMOLOG_DATABASE_URL
if ([string]::IsNullOrWhiteSpace($url)) { throw 'Defina ORCAFACIL_HOMOLOG_DATABASE_URL. Nenhuma senha fica no repositório.' }
if ($url -match '(Password|Pwd)\s*=\s*(SUA_SENHA|password|secret)?\s*($|;)') { throw 'A connection string ainda está com senha de exemplo.' }
$builder = New-Object System.Data.Common.DbConnectionStringBuilder
$builder.set_ConnectionString($url)
$hostName = [string]$builder['Host']
$database = [string]$builder['Database']
if ($hostName -notin @('localhost','127.0.0.1','::1')) { throw "Host '$hostName' recusado. Use apenas um PostgreSQL local descartável." }
if ($database -notmatch '(_homolog|_test)$') { throw "Banco '$database' recusado. Use um nome que termine em _homolog ou _test." }
if ($database -eq 'orcafacil') { throw 'O banco padrão orcafacil não é destino desta preparação.' }
Write-Output "Preparando o banco local '$database' em $hostName."
if ($WhatIf) { return }
& "$PSScriptRoot/update-database.ps1" -ConnectionString $url
if ($LASTEXITCODE -ne 0) { throw 'A atualização do banco de homologação falhou.' }
Write-Output 'Schema aditivo aplicado. Crie as contas de teste pela tela de cadastro. Provedores de IA e pagamento permanecem desligados até haver configuração válida no ambiente.'
