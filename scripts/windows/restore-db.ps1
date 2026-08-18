[CmdletBinding(SupportsShouldProcess,ConfirmImpact='High')] param([Parameter(Mandatory)][string]$Backup,[string]$ConnectionString,[switch]$ConfirmRestore)
. "$PSScriptRoot/common.ps1"; Require-Command 'pg_restore'
if (-not $ConfirmRestore) { throw 'Restauração bloqueada. Revise o destino e repita com -ConfirmRestore.' }
$file=(Resolve-Path $Backup).Path; $db=Get-DatabaseUrl $ConnectionString
if ($PSCmdlet.ShouldProcess('banco PostgreSQL configurado','Restaurar backup sem limpar objetos existentes')) { & pg_restore --dbname=$db --no-owner --no-acl --exit-on-error $file; if ($LASTEXITCODE -ne 0) { throw 'Restauração falhou; consulte a saída do pg_restore.' } }
