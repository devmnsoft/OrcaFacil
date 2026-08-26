param([Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9.-]+$')][string]$HostName)
$ErrorActionPreference = 'Stop'
Write-Host "Consultando DNS público para $HostName (somente leitura)..."
Resolve-DnsName -Name $HostName -ErrorAction Continue | Select-Object Name,Type,IPAddress,NameHost
Resolve-DnsName -Name "_orcafacil-verification.$HostName" -Type TXT -ErrorAction Continue | Select-Object Name,Type,Strings
Write-Host 'Nenhuma alteração foi realizada. Valide o valor TXT exclusivamente no painel seguro.'
