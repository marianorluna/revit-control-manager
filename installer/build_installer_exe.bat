@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%ControlManager.BundleInstaller\ControlManager.BundleInstaller.csproj"
set "BUNDLE=%SCRIPT_DIR%..\bundle\ControlManager.bundle"
set "EMBED_ASSET=%SCRIPT_DIR%ControlManager.BundleInstaller\Assets\ControlManager.bundle.zip"
set "OUT_DIR=%SCRIPT_DIR%dist\installer-exe"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo ERROR: dotnet no esta en PATH.
  exit /b 1
)

if not exist "%PROJECT%" (
  echo ERROR: No se encuentra el proyecto del instalador:
  echo   %PROJECT%
  exit /b 1
)

if not exist "%BUNDLE%\PackageContents.xml" (
  echo ERROR: No se encuentra bundle listo para embeber:
  echo   %BUNDLE%
  echo Ejecuta antes: installer\populate_bundle.bat
  exit /b 1
)

if not exist "%SCRIPT_DIR%ControlManager.BundleInstaller\Assets" mkdir "%SCRIPT_DIR%ControlManager.BundleInstaller\Assets" >nul 2>&1
if exist "%EMBED_ASSET%" del /Q "%EMBED_ASSET%"

echo Generando bundle embebido para el instalador...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%BUNDLE%' -DestinationPath '%EMBED_ASSET%' -Force"
if errorlevel 1 (
  echo ERROR: No se pudo generar Assets\ControlManager.bundle.zip
  exit /b 1
)

if exist "%OUT_DIR%" rmdir /S /Q "%OUT_DIR%"
mkdir "%OUT_DIR%" >nul 2>&1

echo Compilando instalador EXE (self-contained, win-x64^)...
dotnet publish "%PROJECT%" -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o "%OUT_DIR%"
if errorlevel 1 (
  echo ERROR: Fallo dotnet publish del instalador.
  exit /b 1
)

echo.
echo OK: Instalador generado en:
echo   %OUT_DIR%\ControlManager.Installer.exe
exit /b 0
