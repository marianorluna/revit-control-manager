@echo off
setlocal EnableExtensions

echo ========================================
echo  Control Manager - Build All Versions
echo ========================================

set "ROOT=%~dp0"
cd /d "%ROOT%"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] dotnet no esta en el PATH.
  exit /b 1
)

echo [INFO] Restaurando paquetes NuGet...
dotnet restore "%ROOT%ControlManager.sln"
if errorlevel 1 (
  echo [ERROR] Fallo dotnet restore
  exit /b 1
)

for %%V in (2023 2024 2025 2026) do (
  if exist "C:\Program Files\Autodesk\Revit %%V\RevitAPI.dll" (
    echo [INFO] Compilando para Revit %%V (.NET Framework 4.8^)...
    dotnet msbuild "%ROOT%src\ControlManager.Legacy\ControlManager.Legacy.csproj" -p:Configuration=Release%%V -p:Platform=x64 -v:m
    if errorlevel 1 (
      echo [ERROR] Fallo compilando para Revit %%V
    ) else (
      if not exist "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\%%V\"
      copy /y "%ROOT%src\ControlManager.Legacy\ControlManager.addin" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\ControlManager.addin" >nul
      copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\ControlManager.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul
      copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\DocumentFormat.OpenXml.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
      copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\DocumentFormat.OpenXml.Framework.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
      copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\System.IO.Packaging.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
      echo [OK] Revit %%V compilado y copiado al bundle
    )
  ) else (
    echo [SKIP] Revit %%V no instalado
  )
)

if exist "C:\Program Files\Autodesk\Revit 2027\RevitAPI.dll" (
  echo [INFO] Compilando para Revit 2027 (.NET 10^)...
  dotnet build "%ROOT%src\ControlManager.Net10\ControlManager.Net10.csproj" -c Release2027 -p:Platform=x64 --no-restore -v:m
  if errorlevel 1 (
    echo [ERROR] Fallo compilando para Revit 2027
  ) else (
    if not exist "%ROOT%bundle\ControlManager.bundle\Contents\2027\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\2027\"
    copy /y "%ROOT%src\ControlManager.Legacy\ControlManager.addin" "%ROOT%bundle\ControlManager.bundle\Contents\2027\ControlManager.addin" >nul
    copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\ControlManager.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul
    copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\DocumentFormat.OpenXml.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul 2>nul
    copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\DocumentFormat.OpenXml.Framework.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul 2>nul
    copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\System.IO.Packaging.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul 2>nul
    echo [OK] Revit 2027 compilado y copiado al bundle
  )
) else (
  echo [SKIP] Revit 2027 no instalado
)

echo ========================================
echo  Build completado. Revisa los [ERROR] si hay alguno.
echo ========================================
endlocal
exit /b 0
