param([int]$Port=5080,[switch]$Iis)
. "$PSScriptRoot/common.ps1"
$failures=@(); foreach($cmd in @('dotnet','psql')) { if(Get-Command $cmd -ErrorAction SilentlyContinue){Write-Host "[OK] $cmd" -ForegroundColor Green}else{$failures += "$cmd não instalado"} }
if(-not $env:ConnectionStrings__DefaultConnection -and -not $env:ORCAFACIL_DATABASE_URL){$failures += 'ConnectionStrings__DefaultConnection ou ORCAFACIL_DATABASE_URL ausente'}
if(-not $env:ASPNETCORE_URLS){Write-Warning 'ASPNETCORE_URLS não definido; o padrão local será usado.'}
$listener=Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue; if($listener){$failures += "porta $Port já está em uso"}else{Write-Host "[OK] porta $Port livre" -ForegroundColor Green}
try{$probe=Join-Path $RepoRoot '.environment-write-test'; Set-Content $probe 'ok'; Remove-Item $probe; Write-Host '[OK] pasta gravável' -ForegroundColor Green}catch{$failures += 'sem permissão de escrita na pasta'}
if(Get-Command psql -ErrorAction SilentlyContinue -and ($env:ConnectionStrings__DefaultConnection -or $env:ORCAFACIL_DATABASE_URL)){try{& psql (Get-DatabaseUrl '') --no-psqlrc --tuples-only --command 'select 1' | Out-Null; if($LASTEXITCODE -ne 0){throw}; Write-Host '[OK] PostgreSQL acessível' -ForegroundColor Green}catch{$failures += 'PostgreSQL inacessível'}}
if($Iis){$iisFeature=Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -ErrorAction SilentlyContinue; if($iisFeature.State -ne 'Enabled'){$failures += 'IIS não habilitado'}else{Write-Host '[OK] IIS habilitado' -ForegroundColor Green}}
if($failures.Count){$failures|ForEach-Object{Write-Error $_}; exit 1}; Write-Host 'Ambiente pronto.' -ForegroundColor Green
