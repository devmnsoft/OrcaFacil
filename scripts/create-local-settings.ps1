[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$examplePath = Join-Path $repositoryRoot 'src/OrcaFacil.Web/appsettings.Local.example.json'
$localPath = Join-Path $repositoryRoot 'src/OrcaFacil.Web/appsettings.Local.json'
if (-not (Test-Path -LiteralPath $examplePath)) { throw "Arquivo de exemplo não encontrado: $examplePath" }
if (Test-Path -LiteralPath $localPath) {
    Write-Host "A configuração existente não foi sobrescrita: $localPath"
    Write-Host 'Reinicie a aplicação após qualquer alteração.'
    exit 0
}

$hostName = Read-Host 'Host [localhost]'; if ([string]::IsNullOrWhiteSpace($hostName)) { $hostName = 'localhost' }
$port = Read-Host 'Porta [5432]'; if ([string]::IsNullOrWhiteSpace($port)) { $port = '5432' }
$database = Read-Host 'Banco [orcafacil]'; if ([string]::IsNullOrWhiteSpace($database)) { $database = 'orcafacil' }
$username = Read-Host 'Usuário [orcafacil_user]'; if ([string]::IsNullOrWhiteSpace($username)) { $username = 'orcafacil_user' }
$securePassword = Read-Host 'Senha (não será exibida)' -AsSecureString
$password = [Net.NetworkCredential]::new('', $securePassword).Password
if ([string]::IsNullOrWhiteSpace($password)) { throw 'Uma senha válida é obrigatória.' }

$builder = [System.Data.Common.DbConnectionStringBuilder]::new()
$builder['Host'] = $hostName; $builder['Port'] = [int]$port; $builder['Database'] = $database
$builder['Username'] = $username; $builder['Password'] = $password; $builder['Pooling'] = $true; $builder['Timeout'] = 15
$settings = [ordered]@{ ConnectionStrings = [ordered]@{ DefaultConnection = $builder.ConnectionString } }
$settings | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $localPath -Encoding utf8NoBOM
$password = $null; $securePassword.Dispose()
Write-Host "Configuração local criada: $localPath"

$override = [Environment]::GetEnvironmentVariable('ConnectionStrings__DefaultConnection', 'Process')
$userOverride = [Environment]::GetEnvironmentVariable('ConnectionStrings__DefaultConnection', 'User')
$machineOverride = [Environment]::GetEnvironmentVariable('ConnectionStrings__DefaultConnection', 'Machine')
if ($override -or $userOverride -or $machineOverride) {
    Write-Warning 'ConnectionStrings__DefaultConnection está sobrescrevendo o arquivo local (o valor não será exibido).'
    if ($userOverride -and (Read-Host 'Remover a variável somente do escopo User? [s/N]') -match '^[sS]$') {
        [Environment]::SetEnvironmentVariable('ConnectionStrings__DefaultConnection', $null, 'User')
        Write-Host 'Variável removida do escopo User.'
    }
    if ($machineOverride) { Write-Warning 'A variável Machine não foi alterada; remova-a apenas em sessão administrativa confirmada.' }
}

if (Get-Command psql -ErrorAction SilentlyContinue) {
    Write-Host 'Execute scripts/test-database-configuration.ps1 para testar a conexão sem expor a senha.'
} else { Write-Warning 'psql não está disponível; o arquivo foi validado como JSON, mas a conexão não foi aberta.' }
Write-Host 'Reinicie a aplicação para carregar a nova configuração.'
