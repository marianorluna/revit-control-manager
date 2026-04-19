@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "BUNDLE=%APPDATA%\Autodesk\ApplicationPlugins\ControlManager.bundle"

echo === Verificación del bundle instalado ===
echo Ruta: %BUNDLE%
echo.

if not exist "%BUNDLE%" (
  echo El bundle no está instalado en ApplicationPlugins.
  echo Ejecuta primero install_bundle.bat
  exit /b 1
)

echo --- Contenido (árbol simple^) ---
dir /S /B "%BUNDLE%" 2>nul
echo.

echo --- Archivos críticos (XML, íconos, .addin^) ---
set "ERR=0"

call :check "%BUNDLE%\PackageContents.xml" "PackageContents.xml"
call :check "%BUNDLE%\Resources\icon_32.png" "Resources\icon_32.png"
call :check "%BUNDLE%\Resources\icon_16.png" "Resources\icon_16.png"

for %%Y in (2023 2024 2025 2026 2027) do (
  call :check "%BUNDLE%\Contents\%%Y\ControlManager.addin" "Contents\%%Y\ControlManager.addin"
)

echo.
echo --- DLLs por versión (copiar desde build o releases^) ---
for %%Y in (2023 2024 2025 2026 2027) do (
  call :checkdll "%BUNDLE%\Contents\%%Y\ControlManager.dll" "Contents\%%Y\ControlManager.dll"
)

echo.
echo --- Heurística de runtime en DLLs (si existen^) ---
set "CM_BUNDLE=%BUNDLE%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$b=$env:CM_BUNDLE; $years=2023,2024,2025,2026,2027; foreach($y in $years){ $dll=[IO.Path]::Combine($b,'Contents',$y.ToString(),'ControlManager.dll'); if(-not(Test-Path -LiteralPath $dll)){ Write-Host ('  Revit '+$y+': DLL no copiada aún'); continue }; $bytes=[IO.File]::ReadAllBytes($dll); $u=[Text.Encoding]::Unicode.GetString($bytes); $a=[Text.Encoding]::ASCII.GetString($bytes); if($u -match 'net10' -or $a -match 'net10'){ Write-Host ('  Revit '+$y+': indicador net10 / orientativo .NET 10 (2027)') } elseif($a -match 'v4\.0\.30319'){ Write-Host ('  Revit '+$y+': CLR v4 / orientativo .NET Framework 4.8 (2023-2026)') } else { Write-Host ('  Revit '+$y+': runtime no determinado automáticamente') } }"

if %ERR% NEQ 0 (
  echo.
  echo Resumen: hay archivos críticos FALTANTES (revisa OK/FALTA arriba^).
  exit /b 1
)

echo.
echo Resumen: estructura crítica OK. Las DLL se añaden al empaquetar cada versión.
exit /b 0

:check
set "P=%~1"
set "LABEL=%~2"
if exist "%P%" (
  echo OK   %LABEL%
) else (
  echo FALTA %LABEL%
  set "ERR=1"
)
goto :eof

:checkdll
set "P=%~1"
set "LABEL=%~2"
if exist "%P%" (
  echo OK   %LABEL%
) else (
  echo FALTA %LABEL%
)
goto :eof
