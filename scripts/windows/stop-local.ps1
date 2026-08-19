[CmdletBinding(SupportsShouldProcess)]
param()
. "$PSScriptRoot/common.ps1"

$pidFile = Join-Path $RepoRoot 'artifacts/runtime/orcafacil-web.pid'
if (-not (Test-Path $pidFile)) { Write-Host 'Nenhuma execução local registrada.'; return }
$processId = [int](Get-Content $pidFile -Raw)
$process = Get-Process -Id $processId -ErrorAction SilentlyContinue
if ($process -and $PSCmdlet.ShouldProcess("PID $processId", 'Encerrar OrçaFácil')) {
    Stop-Process -Id $processId -ErrorAction Stop
    $process.WaitForExit(10000)
    Write-Host "OrçaFácil encerrado (PID $processId)."
}
Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
