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
dotnet restore "%ROOT%src\ControlManager.Net8\ControlManager.Net8.csproj"
if errorlevel 1 (
  echo [ERROR] Fallo dotnet restore Net8
  exit /b 1
)
dotnet restore "%ROOT%src\ControlManager.Net10\ControlManager.Net10.csproj"
if errorlevel 1 (
  echo [ERROR] Fallo dotnet restore Net10
  exit /b 1
)
REM Legacy usa packages.config / HintPath locales; msbuild Restore cubre PackageReference si aparece.
dotnet msbuild "%ROOT%src\ControlManager.Legacy\ControlManager.Legacy.csproj" -t:Restore -p:Configuration=Release2024 -p:Platform=x64 -v:q


set "BUILD_ERR=0"

REM --- 2023-2024: .NET Framework 4.8 (ControlManager.Legacy) ---
for %%V in (2023 2024) do (
  echo [INFO] Compilando para Revit %%V (.NET Framework 4.8^)...
  dotnet msbuild "%ROOT%src\ControlManager.Legacy\ControlManager.Legacy.csproj" -p:Configuration=Release%%V -p:Platform=x64 -p:DeployRevitAddinToFolder=false -v:m
  if errorlevel 1 (
    echo [ERROR] Fallo compilando para Revit %%V
    set "BUILD_ERR=1"
  ) else (
    if not exist "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\%%V\"
    copy /y "%ROOT%src\ControlManager.Legacy\ControlManager.addin" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\ControlManager.addin" >nul
    copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\ControlManager.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul
    copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\DocumentFormat.OpenXml.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
    copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\DocumentFormat.OpenXml.Framework.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
    copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\System.IO.Packaging.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
    if exist "%ROOT%src\ControlManager.Legacy\bin\Release%%V\docs\privacy-policy.html" (
      if not exist "%ROOT%bundle\ControlManager.bundle\Contents\%%V\docs\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\%%V\docs\"
      copy /y "%ROOT%src\ControlManager.Legacy\bin\Release%%V\docs\privacy-policy.html" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\docs\" >nul
    )
    echo [OK] Revit %%V compilado y copiado al bundle
  )
)

REM --- 2025-2026: .NET 8 (ControlManager.Net8) ---
for %%V in (2025 2026) do (
  echo [INFO] Compilando para Revit %%V (.NET 8^)...
  dotnet build "%ROOT%src\ControlManager.Net8\ControlManager.Net8.csproj" -c Release%%V -p:Platform=x64 -p:DeployRevitAddinToFolder=false -v:m
  if errorlevel 1 (
    echo [ERROR] Fallo compilando para Revit %%V
    set "BUILD_ERR=1"
  ) else (
    if not exist "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\%%V\"
    copy /y "%ROOT%src\ControlManager.Legacy\ControlManager.addin" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\ControlManager.addin" >nul
    copy /y "%ROOT%src\ControlManager.Net8\bin\Release%%V\ControlManager.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul
    copy /y "%ROOT%src\ControlManager.Net8\bin\Release%%V\DocumentFormat.OpenXml.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
    copy /y "%ROOT%src\ControlManager.Net8\bin\Release%%V\DocumentFormat.OpenXml.Framework.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
    copy /y "%ROOT%src\ControlManager.Net8\bin\Release%%V\System.IO.Packaging.dll" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\" >nul 2>nul
    if exist "%ROOT%src\ControlManager.Net8\bin\Release%%V\docs\privacy-policy.html" (
      if not exist "%ROOT%bundle\ControlManager.bundle\Contents\%%V\docs\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\%%V\docs\"
      copy /y "%ROOT%src\ControlManager.Net8\bin\Release%%V\docs\privacy-policy.html" "%ROOT%bundle\ControlManager.bundle\Contents\%%V\docs\" >nul
    )
    echo [OK] Revit %%V compilado y copiado al bundle
  )
)

REM --- 2027: .NET 10 (ControlManager.Net10) ---
echo [INFO] Compilando para Revit 2027 (.NET 10^)...
dotnet build "%ROOT%src\ControlManager.Net10\ControlManager.Net10.csproj" -c Release2027 -p:Platform=x64 -p:DeployRevitAddinToFolder=false -v:m
if errorlevel 1 (
  echo [ERROR] Fallo compilando para Revit 2027
  set "BUILD_ERR=1"
) else (
  if not exist "%ROOT%bundle\ControlManager.bundle\Contents\2027\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\2027\"
  copy /y "%ROOT%src\ControlManager.Legacy\ControlManager.addin" "%ROOT%bundle\ControlManager.bundle\Contents\2027\ControlManager.addin" >nul
  copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\ControlManager.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul
  copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\DocumentFormat.OpenXml.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul 2>nul
  copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\DocumentFormat.OpenXml.Framework.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul 2>nul
  copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\System.IO.Packaging.dll" "%ROOT%bundle\ControlManager.bundle\Contents\2027\" >nul 2>nul
  if exist "%ROOT%src\ControlManager.Net10\bin\Release2027\docs\privacy-policy.html" (
    if not exist "%ROOT%bundle\ControlManager.bundle\Contents\2027\docs\" mkdir "%ROOT%bundle\ControlManager.bundle\Contents\2027\docs\"
    copy /y "%ROOT%src\ControlManager.Net10\bin\Release2027\docs\privacy-policy.html" "%ROOT%bundle\ControlManager.bundle\Contents\2027\docs\" >nul
  )
  echo [OK] Revit 2027 compilado y copiado al bundle
)

echo ========================================
if "%BUILD_ERR%"=="1" (
  echo  Build con errores. Revisa los [ERROR] arriba.
  exit /b 1
)
echo  Build completado OK (2023-2027).
echo ========================================
endlocal
exit /b 0
