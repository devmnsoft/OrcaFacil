param([Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9.-]+$')][string]$HostName)
$ErrorActionPreference = 'Stop'
Import-Module WebAdministration
Get-WebBinding | Where-Object { $_.bindingInformation -like "*:$HostName" } | Select-Object protocol,bindingInformation,sslFlags
Write-Host 'Consulta concluída; nenhum binding foi alterado.'
