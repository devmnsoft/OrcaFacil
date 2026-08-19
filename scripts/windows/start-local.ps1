[CmdletBinding()]
param([int]$Port = 5080, [string]$Environment = 'Development')
. "$PSScriptRoot/common.ps1"

$runtime = Join-Path $RepoRoot 'artifacts/runtime'
$logs = Join-Path $runtime 'logs'
$pidFile = Join-Path $runtime 'orcafacil-web.pid'
New-Item -ItemType Directory -Force $logs | Out-Null
if (Test-Path $pidFile) {
    $existingPid = [int](Get-Content $pidFile -Raw)
    if (Get-Process -Id $existingPid -ErrorAction SilentlyContinue) { throw "OrçaFácil já está em execução (PID $existingPid)." }
    Remove-Item $pidFile -Force
}
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) { throw "A porta $Port já está em uso." }

$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$stdout = Join-Path $logs 'local-stdout.log'
$stderr = Join-Path $logs 'local-stderr.log'
$project = Join-Path $RepoRoot 'src/OrcaFacil.Web/OrcaFacil.Web.csproj'
$process = Start-Process dotnet -ArgumentList @('run','--no-launch-profile','--project',$project) -WorkingDirectory $RepoRoot -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
Set-Content -Path $pidFile -Value $process.Id -Encoding ascii
Write-Host "OrçaFácil iniciado em http://localhost:$Port (PID $($process.Id))."
Write-Host "Logs: $logs"
