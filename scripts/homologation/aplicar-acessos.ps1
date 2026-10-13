# Cria contas de homologação no banco apontado pela configuração local do projeto Web.
# Não use em produção. Reexecutar não duplica, não troca senha e não muda plano existente.
# Senhas novas ficam somente em %TEMP%\orcafacil-homologation-credentials.txt.
param(
    [switch]$IncludeSuperAdmin
)

$ErrorActionPreference = "Stop"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ORCAFACIL_HOMOLOGATION_SEED = "true"
$env:ORCAFACIL_HOMOLOGATION_SEED_EXIT = "true"
if ($IncludeSuperAdmin) {
    $env:ORCAFACIL_ALLOW_LOCAL_SUPERADMIN_SEED = "true"
}

$project = Join-Path $PSScriptRoot "..\..\src\OrcaFacil.Web\OrcaFacil.Web.csproj"
dotnet run --project $project --no-launch-profile
if ($LASTEXITCODE -ne 0) { throw "A criação dos acessos de homologação falhou." }

$credentials = Join-Path $env:TEMP "orcafacil-homologation-credentials.txt"
if (Test-Path $credentials) {
    Write-Host "Credenciais novas: $credentials"
} else {
    Write-Host "Nenhuma credencial nova. Contas existentes foram preservadas ou o banco não tinha plano publicado."
}
