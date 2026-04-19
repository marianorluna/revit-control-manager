@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
set "BUNDLE_SRC=%SCRIPT_DIR%..\bundle\ControlManager.bundle"
set "DEST_ROOT=%APPDATA%\Autodesk\ApplicationPlugins"
set "DEST=%DEST_ROOT%\ControlManager.bundle"

if not exist "%BUNDLE_SRC%\PackageContents.xml" (
  echo ERROR: No se encuentra el bundle en:
  echo   %BUNDLE_SRC%
  exit /b 1
)

echo Instalando Control Manager en la ubicación de ApplicationPlugins (perfil de usuario^)...
echo Origen:  %BUNDLE_SRC%
echo Destino: %DEST%
echo.

if not exist "%DEST_ROOT%" mkdir "%DEST_ROOT%" 2>nul
if exist "%DEST%" (
  echo Eliminando instalación anterior...
  rmdir /S /Q "%DEST%"
  if exist "%DEST%" (
    echo ERROR: No se pudo eliminar la carpeta existente. Cierra Revit e inténtalo de nuevo.
    exit /b 1
  )
)

robocopy "%BUNDLE_SRC%" "%DEST%" /E /NFL /NDL /NJH /NJS /NC /NS /NP
set "RC=%ERRORLEVEL%"
if %RC% GEQ 8 (
  echo ERROR: robocopy falló con código %RC%.
  exit /b 1
)

if not exist "%DEST%\PackageContents.xml" (
  echo ERROR: La copia no contiene PackageContents.xml.
  exit /b 1
)

echo.
echo Instalación correcta. Reinicia Revit para cargar el plugin desde ApplicationPlugins.
exit /b 0
