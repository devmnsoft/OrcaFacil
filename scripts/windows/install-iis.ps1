#Requires -RunAsAdministrator
param([string]$SiteName='OrcaFacil',[string]$PhysicalPath='C:\inetpub\OrcaFacil',[int]$Port=8080)
. "$PSScriptRoot/common.ps1"; Import-Module WebAdministration
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-ManagementConsole -All -NoRestart | Out-Null
New-Item $PhysicalPath -ItemType Directory -Force | Out-Null
if(-not (Test-Path "IIS:\AppPools\$SiteName")){New-WebAppPool $SiteName | Out-Null}; Set-ItemProperty "IIS:\AppPools\$SiteName" managedRuntimeVersion ''
if(-not (Test-Path "IIS:\Sites\$SiteName")){New-Website -Name $SiteName -PhysicalPath $PhysicalPath -Port $Port -ApplicationPool $SiteName | Out-Null}
Write-Host 'Instale também o ASP.NET Core Hosting Bundle compatível com o TargetFramework antes de iniciar o site.' -ForegroundColor Yellow
