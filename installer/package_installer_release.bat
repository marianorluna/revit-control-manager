@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
set "ROOT=%SCRIPT_DIR%.."
set "EXE=%SCRIPT_DIR%dist\installer-exe\ControlManager.Installer.exe"
set "STAGE=%SCRIPT_DIR%dist\release-installer"
set "VERSION=%~1"

if "%VERSION%"=="" (
  echo Uso: package_installer_release.bat vX.Y.Z
  exit /b 1
)

if not exist "%EXE%" (
  echo ERROR: No se encuentra el instalador EXE:
  echo   %EXE%
  echo Ejecuta primero: installer\build_installer_exe.bat
  exit /b 1
)

if exist "%STAGE%" rmdir /S /Q "%STAGE%"
mkdir "%STAGE%" >nul 2>&1

copy /Y "%EXE%" "%STAGE%\ControlManager.Installer.exe" >nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%STAGE%\*' -DestinationPath '%ROOT%\releases\ControlManager_Installer_%VERSION%.zip' -Force"
if errorlevel 1 (
  echo ERROR: No se pudo generar el ZIP del instalador.
  exit /b 1
)

echo.
echo OK: Asset instalador generado:
echo   %ROOT%\releases\ControlManager_Installer_%VERSION%.zip
echo.
echo Contenido:
echo - ControlManager.Installer.exe
echo.
echo Uso para usuario final:
echo - Un clic (toggle instalar/desinstalar): ControlManager.Installer.exe
echo - Instalar forzado:                       ControlManager.Installer.exe --install
echo - Desinstalar: ControlManager.Installer.exe --uninstall
exit /b 0
