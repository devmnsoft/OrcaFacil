param([string]$Output = 'artifacts/publish/orcafacil-web')
. "$PSScriptRoot/common.ps1"; Require-Command 'dotnet'
$out = Join-Path $RepoRoot $Output
Push-Location $RepoRoot
try { dotnet publish '.\src\OrcaFacil.Web\OrcaFacil.Web.csproj' -c Release -o $out; if ($LASTEXITCODE -ne 0) { throw 'Falha no publish.' } } finally { Pop-Location }
Write-Host "Release publicada em $out" -ForegroundColor Green
