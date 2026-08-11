[CmdletBinding()] param([string]$Database='orcafacil',[string]$User='postgres',[string]$HostName='localhost',[int]$Port=5432)
$ErrorActionPreference='Stop'; foreach($tool in @('dotnet','node','psql')){if(!(Get-Command $tool -ErrorAction SilentlyContinue)){throw "$tool não encontrado no PATH."}}
Write-Host 'Crie o banco se necessário: createdb -h HOST -U USER orcafacil'; $apply=Read-Host 'Aplicar database/script_completop.sql agora? (s/N)'
if($apply -eq 's'){$env:PGPASSWORD=$null;& psql -h $HostName -p $Port -U $User -d $Database -v ON_ERROR_STOP=1 -f "$PSScriptRoot\..\database\script_completop.sql";if($LASTEXITCODE){throw 'Script SQL falhou.'}}
Write-Host 'Configure sem versionar:'; Write-Host 'dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=..." --project src/OrcaFacil.Web'
& dotnet restore "$PSScriptRoot\..\OrcaFacil.sln"; if($LASTEXITCODE){throw 'restore falhou'};& dotnet build "$PSScriptRoot\..\OrcaFacil.sln" -c Debug --no-restore;if($LASTEXITCODE){throw 'build falhou'}
Write-Host 'Pronto. Execute dotnet run --project src/OrcaFacil.Web e acesse https://localhost:7064'
