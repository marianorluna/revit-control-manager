#Requires -Version 5.1
<#
.SYNOPSIS
  Copia las DLL compiladas (y dependencias en la misma carpeta de salida) al bundle App Store.
.DESCRIPTION
  Origen: salidas de ControlManager.Legacy (2023-2026, .NET Framework 4.8) y ControlManager.Net10 (2027, .NET 10).
  Destino: bundle\ControlManager.bundle\Contents\<año>\
  Usa -Build para invocar MSBuild/dotnet antes de copiar (requiere Revit API en las rutas del .csproj).
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

function Get-LegacySourceDir {
    param([int] $Year)
    $base = Join-Path $RepoRoot 'src\ControlManager.Legacy'
    if ($Configuration -eq 'Release') {
        $candidates = @(
            (Join-Path $base "bin\Release$Year"),
            (Join-Path $base "bin\x64\Release$Year")
        )
    }
    else {
        $candidates = @(
            (Join-Path $base "bin\x64\Debug$Year"),
            (Join-Path $base "bin\Debug$Year")
        )
    }
    foreach ($d in $candidates) {
        $dll = Join-Path $d 'ControlManager.dll'
        if (Test-Path -LiteralPath $dll) { return $d }
    }
    return $null
}

function Get-Net10SourceDir {
    $base = Join-Path $RepoRoot 'src\ControlManager.Net10'
    if ($Configuration -eq 'Release') {
        $candidates = @(
            (Join-Path $base 'bin\Release2027'),
            (Join-Path $base 'bin\x64\Release2027'),
            (Join-Path $base 'bin\Release2027\net10.0-windows'),
            (Join-Path $base 'bin\x64\Release2027\net10.0-windows')
        )
    }
    else {
        $candidates = @(
            (Join-Path $base 'bin\x64\Debug2027'),
            (Join-Path $base 'bin\Debug2027'),
            (Join-Path $base 'bin\Debug2027\net10.0-windows'),
            (Join-Path $base 'bin\x64\Debug2027\net10.0-windows')
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
    $dlls = Get-ChildItem -LiteralPath $SourceDir -Filter '*.dll' -File -ErrorAction SilentlyContinue
    if (-not $dlls) {
        return $false
    }
    foreach ($f in $dlls) {
        Copy-Item -LiteralPath $f.FullName -Destination $dest -Force
    }
    return $true
}

if ($Build) {
    $msbuild = Get-MsBuildExe
    if (-not $msbuild) {
        Write-Error 'No se encontró MSBuild (instala Visual Studio o Build Tools). No se puede usar -Build.'
    }
    Write-Host 'Compilando ControlManager.Legacy (2023-2026)...' -ForegroundColor Cyan
    foreach ($y in 2023..2026) {
        $cfg = "{0}{1}" -f $Configuration, $y
        & $msbuild $LegacyProj /t:Restore,Build /p:Configuration=$cfg /p:Platform=x64 /v:minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Compilación $cfg falló (¿Revit $y instalado en la ruta del .csproj?)."
        }
    }
    Write-Host 'Compilando ControlManager.Net10 (2027)...' -ForegroundColor Cyan
    Push-Location $RepoRoot
    try {
        dotnet build $Net10Proj -c "${Configuration}2027" -p:Platform=x64 --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Warning 'Compilación Release2027/Debug2027 de Net10 falló (¿Revit 2027 en C:\Program Files\Autodesk\Revit 2027\?).'
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "`nCopiando DLL al bundle: $BundleContents`n" -ForegroundColor Cyan
$anyOk = $false
foreach ($y in 2023..2026) {
    $src = Get-LegacySourceDir -Year $y
    $label = "Revit $y ($Configuration)"
    if (-not $src) {
        Write-Host "OMITIDO  $label - no hay ControlManager.dll en salida. Compila Release$y o Debug$y." -ForegroundColor Yellow
        continue
    }
    if (Publish-DllFolderToBundle -SourceDir $src -DestYearFolder $y) {
        Write-Host "OK       $label - desde $src" -ForegroundColor Green
        $anyOk = $true
    }
}

$src27 = Get-Net10SourceDir
if (-not $src27) {
    Write-Host "OMITIDO  Revit 2027 ($Configuration) - no hay ControlManager.dll en salida Net10." -ForegroundColor Yellow
}
elseif (Publish-DllFolderToBundle -SourceDir $src27 -DestYearFolder '2027') {
    Write-Host "OK       Revit 2027 ($Configuration) - desde $src27" -ForegroundColor Green
    $anyOk = $true
}

if (-not $anyOk) {
    Write-Host "`nNinguna DLL se copió. Compila en Visual Studio o ejecuta con -Build (requiere Revit instalado)." -ForegroundColor Red
    exit 1
}

Write-Host "`nListo. Opcional: ejecuta install_bundle.bat y luego test_bundle.bat." -ForegroundColor Cyan
exit 0
