#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$siteName = Read-Host 'Nome do site no IIS'
$site = Get-Website -Name $siteName -ErrorAction Stop
$poolName = $site.ApplicationPool
$secureConnection = Read-Host 'Connection string PostgreSQL' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureConnection)
try {
    $connection = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $siteName `
        -Filter 'system.webServer/aspNetCore/environmentVariables' -Name '.' `
        -Value @{ name = 'ConnectionStrings__DefaultConnection'; value = $connection }
} finally {
    if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    $connection = $null
}

if ((Read-Host "Reciclar o Application Pool '$poolName'? (S/N)") -match '^[Ss]') {
    Restart-WebAppPool -Name $poolName
}

$binding = Get-WebBinding -Name $siteName -Protocol 'https' | Select-Object -First 1
if (-not $binding) { $binding = Get-WebBinding -Name $siteName -Protocol 'http' | Select-Object -First 1 }
$protocol = $binding.protocol
$hostName = if ($binding.bindingInformation.Split(':')[2]) { $binding.bindingInformation.Split(':')[2] } else { 'localhost' }
$port = $binding.bindingInformation.Split(':')[1]
$uri = "${protocol}://${hostName}:${port}/health/ready"
try {
    $response = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 20
    Write-Host "Readiness validado com status HTTP $($response.StatusCode)."
} catch {
    Write-Warning "Não foi possível validar $uri. Consulte o health check após a aplicação iniciar."
}
