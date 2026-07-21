@echo off
setlocal
set PGHOST=localhost
set PGPORT=5432
set PGDATABASE=orcafacil
set PGUSER=orcafacil_user
set PSQL_PATH=psql
set SCRIPT_FILE=%~dp0script_completop.sql

echo Executando script completo do OrçaFácil em %PGHOST%:%PGPORT%/%PGDATABASE% com usuario %PGUSER%.
echo A senha sera solicitada pelo psql quando necessario.
%PSQL_PATH% -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -f "%SCRIPT_FILE%"
pause
