param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "orcafacil",
    [string]$User = "orcafacil_user",
    [string]$PsqlPath = "psql",
    [string]$ScriptFile = "$PSScriptRoot/script_completop.sql"
)

Write-Host "Executando script completo do OrçaFácil em $HostName`:$Port/$Database com usuário $User."
Write-Host "A senha será solicitada pelo psql quando necessário."
& $PsqlPath -h $HostName -p $Port -U $User -d $Database -f $ScriptFile
exit $LASTEXITCODE
