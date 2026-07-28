[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
function Ask([string]$Prompt, [string]$Default) { $v = Read-Host "$Prompt [$Default]"; if ([string]::IsNullOrWhiteSpace($v)) { $Default } else { $v } }
function Plain([Security.SecureString]$Value) { $ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value); try {[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)} finally {[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)} }
$hostName=Ask 'Host' 'localhost'; $port=Ask 'Porta' '5432'; $admin=Ask 'Usuário administrador' 'postgres'
$adminSecure=Read-Host 'Senha administrativa' -AsSecureString
$dbName=Ask 'Nome do banco' 'orcafacil'; $appUser=Ask 'Usuário da aplicação' 'orcafacil_user'
$appSecure=Read-Host 'Senha da aplicação' -AsSecureString
if ($dbName -notmatch '^[a-zA-Z_][a-zA-Z0-9_]*$' -or $appUser -notmatch '^[a-zA-Z_][a-zA-Z0-9_]*$') { throw 'Banco e usuário devem ser identificadores PostgreSQL simples.' }
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw 'psql não encontrado no PATH.' }
$adminPassword=Plain $adminSecure; $appPassword=Plain $appSecure
try {
  $env:PGPASSWORD=$adminPassword
  $roleExists=& psql -X -qAt -h $hostName -p $port -U $admin -d postgres -v role_name=$appUser -c "SELECT 1 FROM pg_roles WHERE rolname = :'role_name'"
  if ($roleExists -ne '1') {
    $escaped=$appPassword.Replace("'", "''")
    "CREATE ROLE `"$appUser`" LOGIN PASSWORD '$escaped';" | & psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $admin -d postgres | Out-Null
  } elseif ((Read-Host 'O role existe. Alterar sua senha? [s/N]') -match '^[sS]$') {
    $escaped=$appPassword.Replace("'", "''")
    "ALTER ROLE `"$appUser`" PASSWORD '$escaped';" | & psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $admin -d postgres | Out-Null
  }
  $dbExists=& psql -X -qAt -h $hostName -p $port -U $admin -d postgres -v db_name=$dbName -c "SELECT 1 FROM pg_database WHERE datname = :'db_name'"
  if ($dbExists -ne '1') { & createdb -h $hostName -p $port -U $admin --owner=$appUser $dbName }
  & psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $admin -d $dbName -v role_name=$appUser -c "ALTER DATABASE :\"DBNAME\" OWNER TO :\"role_name\"; GRANT CONNECT ON DATABASE :\"DBNAME\" TO :\"role_name\"" | Out-Null
  $connection="Host=$hostName;Port=$port;Database=$dbName;Username=$appUser;Password=$appPassword;Pooling=true;Timeout=15;Command Timeout=30;SSL Mode=Prefer;Search Path=orcafacil,public"
  dotnet user-secrets init --project src/OrcaFacil.Web | Out-Null
  dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $connection --project src/OrcaFacil.Web | Out-Null
  $env:ConnectionStrings__DefaultConnection=$connection
  dotnet ef database update --project src/OrcaFacil.Persistence --startup-project src/OrcaFacil.Web
  $env:PGPASSWORD=$appPassword
  & psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $appUser -d $dbName -c 'select current_database(), current_user' | Out-Null
  Write-Host "Banco local pronto: host=$hostName port=$port database=$dbName user=$appUser (segredo omitido)." -ForegroundColor Green
} finally {
  Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
  $adminPassword=$null; $appPassword=$null; $escaped=$null; $connection=$null
}
