@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "DEST=%APPDATA%\Autodesk\ApplicationPlugins\ControlManager.bundle"
set "ADDINS2027=%APPDATA%\Autodesk\Revit\Addins\2027"

set "HAD=0"
if exist "%DEST%" set "HAD=1"
if exist "%ADDINS2027%\ControlManager.addin" set "HAD=1"

if "%HAD%"=="0" (
  echo No hay instalación en:
  echo   %DEST%
  echo   %ADDINS2027%\ControlManager.addin
  echo Nada que eliminar.
  exit /b 0
)

echo Se eliminará:
if exist "%DEST%" echo   %DEST%
if exist "%ADDINS2027%\ControlManager.addin" echo   archivos ControlManager en %ADDINS2027%
echo.
set /p "CONFIRM=¿Continuar? (S/N): "
if /I not "%CONFIRM%"=="S" (
  echo Cancelado.
  exit /b 0
)

if exist "%DEST%" (
  rmdir /S /Q "%DEST%"
  if exist "%DEST%" (
    echo ERROR: No se pudo eliminar el bundle. Cierra Revit e inténtalo de nuevo.
    exit /b 1
  )
)

if exist "%ADDINS2027%\ControlManager.addin" del /Q "%ADDINS2027%\ControlManager.addin" 2>nul
if exist "%ADDINS2027%\ControlManager.dll" del /Q "%ADDINS2027%\ControlManager.dll" 2>nul
if exist "%ADDINS2027%\ControlManager.pdb" del /Q "%ADDINS2027%\ControlManager.pdb" 2>nul
if exist "%ADDINS2027%\DocumentFormat.OpenXml.dll" del /Q "%ADDINS2027%\DocumentFormat.OpenXml.dll" 2>nul
if exist "%ADDINS2027%\DocumentFormat.OpenXml.Framework.dll" del /Q "%ADDINS2027%\DocumentFormat.OpenXml.Framework.dll" 2>nul
if exist "%ADDINS2027%\System.IO.Packaging.dll" del /Q "%ADDINS2027%\System.IO.Packaging.dll" 2>nul
if exist "%ADDINS2027%\docs" rmdir /S /Q "%ADDINS2027%\docs" 2>nul

echo Eliminación completada.
exit /b 0
