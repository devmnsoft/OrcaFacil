[CmdletBinding()]
param(
  [Alias('Host')][string]$HostName = 'localhost',
  [ValidateRange(1, 65535)][int]$Port = 5432,
  [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Database,
  [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$User
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$psql = Get-Command psql -ErrorAction SilentlyContinue
if (!$psql) { throw 'psql não foi encontrado no PATH. Instale as ferramentas cliente do PostgreSQL e tente novamente.' }

$scripts = @(
  (Join-Path $root 'database/script_completop.sql'),
  (Join-Path $root 'database/patch_release_candidate_schema.sql')
)
foreach ($script in $scripts) { if (!(Test-Path $script -PathType Leaf)) { throw "Script obrigatório ausente: $script" } }

Write-Host "Atualizando '$Database' em ${HostName}:$Port como '$User'." -ForegroundColor Cyan
Write-Host 'A senha será solicitada pelo psql e não será armazenada por este script.'
$started = Get-Date
foreach ($script in $scripts) {
  Write-Host "Executando $([IO.Path]::GetFileName($script))..."
  & $psql.Source -X -W -h $HostName -p $Port -U $User -d $Database -v ON_ERROR_STOP=1 -f $script
  if ($LASTEXITCODE -ne 0) {
    throw "Atualização interrompida em $([IO.Path]::GetFileName($script)). Nenhum banco foi apagado ou recriado. Consulte a mensagem do PostgreSQL acima."
  }
}
$elapsed = (Get-Date) - $started
Write-Host ("Atualização concluída com segurança em {0:n1}s. 2 scripts aplicados; banco e dados preservados." -f $elapsed.TotalSeconds) -ForegroundColor Green
