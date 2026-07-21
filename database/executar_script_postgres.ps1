param(
    [string]$HostName = "localhost",
    [Alias("Host")][string]$Server,
    [int]$Port = 5432,
    [string]$Database = "orcafacil",
    [string]$User = "orcafacil_user"
)
if ($Server) { $HostName = $Server }
$scriptPath = Join-Path $PSScriptRoot "script_completop.sql"
psql -h $HostName -p $Port -U $User -d $Database -f $scriptPath
