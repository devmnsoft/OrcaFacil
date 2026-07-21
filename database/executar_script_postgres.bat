@echo off
set PGHOST=localhost
set PGPORT=5432
set PGDATABASE=orcafacil
set PGUSER=orcafacil_user

echo Executando database\script_completop.sql...
psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -f "%~dp0script_completop.sql"

pause
