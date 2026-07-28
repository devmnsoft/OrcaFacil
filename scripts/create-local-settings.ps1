$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$examplePath = Join-Path $repositoryRoot 'src/OrcaFacil.Web/appsettings.Local.example.json'
$localPath = Join-Path $repositoryRoot 'src/OrcaFacil.Web/appsettings.Local.json'

if (-not (Test-Path -LiteralPath $localPath)) {
    Copy-Item -LiteralPath $examplePath -Destination $localPath
    Write-Host 'Configuração local criada. Abra o arquivo abaixo, informe a senha do PostgreSQL e reinicie o OrçaFácil.'
} else {
    Write-Host 'A configuração local já existe e não foi sobrescrita.'
}

Write-Host $localPath
try { Start-Process -FilePath $localPath | Out-Null } catch { Write-Host 'Não foi possível abrir o editor automaticamente.' }
Write-Host 'Alterações na conexão exigem reiniciar a aplicação.'
