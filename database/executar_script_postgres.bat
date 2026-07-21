@echo off
setlocal
cd /d %~dp0
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f script_completop.sql
endlocal
