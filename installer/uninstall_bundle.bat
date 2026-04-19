@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "DEST=%APPDATA%\Autodesk\ApplicationPlugins\ControlManager.bundle"

if not exist "%DEST%" (
  echo No hay instalación en:
  echo   %DEST%
  echo Nada que eliminar.
  exit /b 0
)

echo Se eliminará:
echo   %DEST%
echo.
set /p "CONFIRM=¿Continuar? (S/N): "
if /I not "%CONFIRM%"=="S" (
  echo Cancelado.
  exit /b 0
)

rmdir /S /Q "%DEST%"
if exist "%DEST%" (
  echo ERROR: No se pudo eliminar la carpeta. Cierra Revit e inténtalo de nuevo.
  exit /b 1
)

echo Eliminación completada.
exit /b 0
