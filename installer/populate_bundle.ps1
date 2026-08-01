#Requires -Version 5.1
<#
.SYNOPSIS
  Copia las DLL compiladas (y dependencias en la misma carpeta de salida) al bundle App Store.
.DESCRIPTION
  Origen:
    - ControlManager.Legacy  (2023-2024, .NET Framework 4.8)
    - ControlManager.Net8    (2025-2026, .NET 8)
    - ControlManager.Net10   (2027, .NET 10)
  Destino: bundle\ControlManager.bundle\Contents\<año>\
  Usa -Build para invocar MSBuild/dotnet antes de copiar.
.PARAMETER Configuration
  Release (por defecto) o Debug: selecciona carpetas bin\Release* o bin\x64\Debug*.
.PARAMETER Build
  Si está presente, compila cada configuración antes de copiar.
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',

    [switch] $Build
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..')).Path
$BundleContents = Join-Path $RepoRoot 'bundle\ControlManager.bundle\Contents'
$LegacyProj = Join-Path $RepoRoot 'src\ControlManager.Legacy\ControlManager.Legacy.csproj'
$Net8Proj = Join-Path $RepoRoot 'src\ControlManager.Net8\ControlManager.Net8.csproj'
$Net10Proj = Join-Path $RepoRoot 'src\ControlManager.Net10\ControlManager.Net10.csproj'

function Get-MsBuildExe {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $installationPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($installationPath) {
            $candidates = @(
                (Join-Path $installationPath 'MSBuild\Current\Bin\MSBuild.exe'),
                (Join-Path $installationPath 'MSBuild\15.0\Bin\MSBuild.exe')
            )
            foreach ($c in $candidates) {
                if (Test-Path -LiteralPath $c) { return $c }
            }
        }
    }
    $fallback = "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path -LiteralPath $fallback) { return $fallback }
    return $null
}

function Get-SourceDir {
    param(
        [string] $ProjectBase,
        [int] $Year
    )
    if ($Configuration -eq 'Release') {
        $candidates = @(
            (Join-Path $ProjectBase "bin\Release$Year"),
            (Join-Path $ProjectBase "bin\x64\Release$Year")
        )
    }
    else {
        $candidates = @(
            (Join-Path $ProjectBase "bin\x64\Debug$Year"),
            (Join-Path $ProjectBase "bin\Debug$Year")
        )
    }
    foreach ($d in $candidates) {
        $dll = Join-Path $d 'ControlManager.dll'
        if (Test-Path -LiteralPath $dll) { return $d }
    }
    return $null
}

function Publish-DllFolderToBundle {
    param(
        [string] $SourceDir,
        [string] $DestYearFolder
    )
    $dest = Join-Path $BundleContents $DestYearFolder
    if (-not (Test-Path -LiteralPath $dest)) {
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
    }

    # Limpia basura de empaquetados previos (carpetas NuGet, DLLs viejas)
    Get-ChildItem -LiteralPath $dest -Force -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.Name -eq 'ControlManager.addin') { return }
        Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }

    $dlls = Get-ChildItem -LiteralPath $SourceDir -Filter '*.dll' -File -ErrorAction SilentlyContinue
    if (-not $dlls) {
        return $false
    }
    foreach ($f in $dlls) {
        Copy-Item -LiteralPath $f.FullName -Destination $dest -Force
    }

    $privacySrcCandidates = @(
        (Join-Path $SourceDir 'docs\privacy-policy.html'),
        (Join-Path $RepoRoot 'docs\privacy-policy.html')
    )
    foreach ($privacySrc in $privacySrcCandidates) {
        if (Test-Path -LiteralPath $privacySrc) {
            $docsDest = Join-Path $dest 'docs'
            New-Item -ItemType Directory -Path $docsDest -Force | Out-Null
            Copy-Item -LiteralPath $privacySrc -Destination (Join-Path $docsDest 'privacy-policy.html') -Force
            break
        }
    }

    return $true
}

if ($Build) {
    Write-Host 'Compilando via build_all.bat...' -ForegroundColor Cyan
    $buildAll = Join-Path $RepoRoot 'build_all.bat'
    & cmd /c "`"$buildAll`""
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'build_all.bat reportó errores; se intentará copiar lo que exista.'
    }
}

Write-Host "`nCopiando DLL al bundle: $BundleContents`n" -ForegroundColor Cyan
$anyOk = $false

# 2023-2024 Legacy
foreach ($y in 2023..2024) {
    $src = Get-SourceDir -ProjectBase (Join-Path $RepoRoot 'src\ControlManager.Legacy') -Year $y
    $label = "Revit $y ($Configuration, net48)"
    if (-not $src) {
        Write-Host "OMITIDO  $label - no hay ControlManager.dll. Compila Release$y." -ForegroundColor Yellow
        continue
    }
    if (Publish-DllFolderToBundle -SourceDir $src -DestYearFolder $y) {
        Write-Host "OK       $label - desde $src" -ForegroundColor Green
        $anyOk = $true
    }
}

# 2025-2026 Net8
foreach ($y in 2025..2026) {
    $src = Get-SourceDir -ProjectBase (Join-Path $RepoRoot 'src\ControlManager.Net8') -Year $y
    $label = "Revit $y ($Configuration, net8)"
    if (-not $src) {
        Write-Host "OMITIDO  $label - no hay ControlManager.dll. Compila Release$y." -ForegroundColor Yellow
        continue
    }
    if (Publish-DllFolderToBundle -SourceDir $src -DestYearFolder $y) {
        Write-Host "OK       $label - desde $src" -ForegroundColor Green
        $anyOk = $true
    }
}

# 2027 Net10
$src27 = Get-SourceDir -ProjectBase (Join-Path $RepoRoot 'src\ControlManager.Net10') -Year 2027
if (-not $src27) {
    Write-Host "OMITIDO  Revit 2027 ($Configuration, net10) - no hay ControlManager.dll." -ForegroundColor Yellow
}
elseif (Publish-DllFolderToBundle -SourceDir $src27 -DestYearFolder '2027') {
    Write-Host "OK       Revit 2027 ($Configuration, net10) - desde $src27" -ForegroundColor Green
    $anyOk = $true
}

# Asegura .addin en cada carpeta de año
$addinSrc = Join-Path $RepoRoot 'src\ControlManager.Legacy\ControlManager.addin'
foreach ($y in 2023..2027) {
    $destAddin = Join-Path $BundleContents "$y\ControlManager.addin"
    $destDir = Split-Path $destAddin -Parent
    if (-not (Test-Path -LiteralPath $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    if (Test-Path -LiteralPath $addinSrc) {
        Copy-Item -LiteralPath $addinSrc -Destination $destAddin -Force
    }
}

if (-not $anyOk) {
    Write-Host "`nNinguna DLL se copió. Ejecuta build_all.bat o populate_bundle.bat -Build." -ForegroundColor Red
    exit 1
}

# Verificación: Packaging no debe ser 8.0.0
Write-Host "`nVerificando System.IO.Packaging (no 8.0.0)..." -ForegroundColor Cyan
$packagingOk = $true
foreach ($y in 2023..2027) {
    $packDll = Join-Path $BundleContents "$y\System.IO.Packaging.dll"
    if (-not (Test-Path -LiteralPath $packDll)) {
        Write-Host "  Revit $y`: System.IO.Packaging.dll ausente (puede ser OK si viene del runtime)" -ForegroundColor Yellow
        continue
    }
    $ver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($packDll).FileVersion
    $asmVer = [System.Reflection.AssemblyName]::GetAssemblyName($packDll).Version.ToString()
    if ($asmVer.StartsWith('8.0.0')) {
        Write-Host "  Revit $y`: FALLO Packaging $asmVer (vulnerabilidad App Store)" -ForegroundColor Red
        $packagingOk = $false
    }
    else {
        Write-Host "  Revit $y`: Packaging $asmVer OK" -ForegroundColor Green
    }
}

if (-not $packagingOk) {
    Write-Host "`nCorrige System.IO.Packaging antes de enviar a App Store." -ForegroundColor Red
    exit 1
}

Write-Host "`nListo. Siguiente: installer\build_installer_exe.bat" -ForegroundColor Cyan
exit 0
