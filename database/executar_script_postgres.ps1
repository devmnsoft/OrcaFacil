param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "orcafacil",
    [string]$User = "orcafacil_user",
    [string]$ScriptPath = "$PSScriptRoot/script_completop.sql"
)

Write-Host "Executando $ScriptPath em ${HostName}:$Port/$Database..."
psql -h $HostName -p $Port -U $User -d $Database -f $ScriptPath
