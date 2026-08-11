@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-iis.ps1" %*
exit /b %ERRORLEVEL%
