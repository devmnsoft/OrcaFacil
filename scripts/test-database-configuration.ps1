[CmdletBinding()]
param([string]$BaseUrl = 'https://localhost:5001')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$localPath = Join-Path $root 'src/OrcaFacil.Web/appsettings.Local.json'
$environmentValue = [Environment]::GetEnvironmentVariable('ConnectionStrings__DefaultConnection')
$source = if ($environmentValue) { 'EnvironmentVariable' } elseif (Test-Path $localPath) { 'LocalJson' } else { 'Missing' }
$connectionString = if ($environmentValue) { $environmentValue } elseif (Test-Path $localPath) {
    (Get-Content $localPath -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection
} else { $null }
if (-not $connectionString) { throw 'Configuração efetiva ausente.' }
$builder = [System.Data.Common.DbConnectionStringBuilder]::new(); $builder.ConnectionString = $connectionString
Write-Host "origem: $source"
foreach ($entry in @(@('host','Host'),@('porta','Port'),@('banco','Database'),@('usuário','Username'))) {
    $value = if ($builder.ContainsKey($entry[1])) { $builder[$entry[1]] } else { '' }; Write-Host "$($entry[0]): $value"
}
$passwordConfigured = $builder.ContainsKey('Password') -and -not [string]::IsNullOrWhiteSpace([string]$builder['Password'])
Write-Host "passwordConfigured: $passwordConfigured"
if (Get-Command psql -ErrorAction SilentlyContinue) {
    $env:PGPASSWORD = [string]$builder['Password']
    & psql -h $builder['Host'] -p $builder['Port'] -U $builder['Username'] -d $builder['Database'] -c 'select current_user, current_database();'
    Write-Host "resultado da conexão: $(if ($LASTEXITCODE -eq 0) {'sucesso'} else {'falha'})"
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
} else { Write-Host 'resultado da conexão: psql indisponível' }
try { $ready = Invoke-RestMethod "$BaseUrl/health/ready" -SkipCertificateCheck; Write-Host "readiness: $($ready.status)" }
catch { Write-Host 'readiness: indisponível' }
