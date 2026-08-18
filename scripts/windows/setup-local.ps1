param([string]$ConnectionString)
. "$PSScriptRoot/common.ps1"; & "$PSScriptRoot/check-environment.ps1"; & "$PSScriptRoot/update-database.ps1" -ConnectionString $ConnectionString; Push-Location $RepoRoot
try { dotnet restore OrcaFacil.sln; if($LASTEXITCODE -ne 0){throw 'Restore falhou.'}; dotnet build src/OrcaFacil.Web/OrcaFacil.Web.csproj -c Debug --no-restore; if($LASTEXITCODE -ne 0){throw 'Build falhou.'} } finally { Pop-Location }
