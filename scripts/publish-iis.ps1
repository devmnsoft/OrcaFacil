[CmdletBinding()]
param([string]$Destination = "$PSScriptRoot\..\artifacts\iis", [string]$Project = "$PSScriptRoot\..\src\OrcaFacil.Web\OrcaFacil.Web.csproj")
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot\..").Path
$stage = Join-Path $root 'artifacts\iis-stage'
$logDir = Join-Path $root 'artifacts\publish-logs'; New-Item $logDir -ItemType Directory -Force | Out-Null
$log = Join-Path $logDir ("publish-{0}.log" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
Start-Transcript -Path $log
try {
  if ([IO.Path]::GetFullPath($Destination).TrimEnd('\') -in @([IO.Path]::GetPathRoot($Destination).TrimEnd('\'), $root.TrimEnd('\'))) { throw 'Destino inseguro para limpeza.' }
  $saved = $null; $production = Join-Path $Destination 'appsettings.Production.json'
  if (Test-Path $production) { $saved = Join-Path $env:TEMP ("orcafacil-production-{0}.json" -f [guid]::NewGuid()); Copy-Item $production $saved }
  Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue; New-Item $stage -ItemType Directory -Force | Out-Null
  & dotnet restore (Join-Path $root 'OrcaFacil.sln'); if ($LASTEXITCODE) { throw 'restore falhou' }
  & dotnet build (Join-Path $root 'OrcaFacil.sln') -c Release --no-restore; if ($LASTEXITCODE) { throw 'build falhou' }
  & dotnet publish $Project -c Release --no-build -o $stage; if ($LASTEXITCODE) { throw 'publish falhou' }
  foreach ($required in @('web.config','wwwroot','OrcaFacil.Web.dll')) { if (!(Test-Path (Join-Path $stage $required))) { throw "Publicação inválida: $required ausente." } }
  if (!(Get-ChildItem $stage -Recurse -Filter '*.Views.dll' -ErrorAction SilentlyContinue) -and !(Get-ChildItem $stage -Recurse -Filter '*.cshtml' -ErrorAction SilentlyContinue)) { throw 'Razor compilado/publicado não encontrado.' }
  New-Item $Destination -ItemType Directory -Force | Out-Null
  Get-ChildItem $Destination -Force | Where-Object Name -ne 'appsettings.Production.json' | Remove-Item -Recurse -Force
  Copy-Item "$stage\*" $Destination -Recurse -Force
  if ($saved) { Copy-Item $saved $production -Force; Remove-Item $saved -Force }
  Write-Host "Publicação concluída em: $([IO.Path]::GetFullPath($Destination))" -ForegroundColor Green
  Write-Host 'Configure o App Pool como No Managed Code e ASPNETCORE_ENVIRONMENT=Production.'
} finally { Stop-Transcript }
