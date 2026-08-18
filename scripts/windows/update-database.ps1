[CmdletBinding(SupportsShouldProcess)] param([string]$ConnectionString,[string]$Patch = 'database/script_completop.sql')
. "$PSScriptRoot/common.ps1"
$db = Get-DatabaseUrl $ConnectionString
$file = (Resolve-Path (Join-Path $RepoRoot $Patch)).Path
if ($file -notlike "$RepoRoot\database\*.sql") { throw 'O patch deve estar dentro de database/.' }
if ($PSCmdlet.ShouldProcess($file, 'Aplicar atualização aditiva no PostgreSQL')) { Invoke-PsqlFile $db $file }
