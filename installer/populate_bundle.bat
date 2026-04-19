@echo off
setlocal EnableExtensions
chcp 65001 >nul
REM Copia DLL compiladas al bundle (ver populate_bundle.ps1). Opciones: Release (defecto), Debug, -Build
set "SCRIPT_DIR=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%populate_bundle.ps1" %*
exit /b %ERRORLEVEL%
