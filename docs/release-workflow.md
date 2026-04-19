# Release Workflow (3-5 min)

Guia corta para generar y publicar una release del plugin sin commitear binarios al repositorio.

## Convencion de versionado

- Formato: `vMAJOR.MINOR.PATCH` (SemVer).
- `MAJOR`: cambios incompatibles o ruptura de compatibilidad.
- `MINOR`: nuevas funcionalidades compatibles.
- `PATCH`: correcciones sin cambios funcionales grandes.
- El nombre del ZIP debe seguir: `ControlManager_vX.Y.Z.zip`.

## Checklist rapido

- [ ] Actualizar `CHANGELOG.md` con la nueva version.
- [ ] Compilar en Release: `build_all.bat`.
- [ ] Poblar DLLs del bundle: `installer/populate_bundle.bat`.
- [ ] Validar estructura del bundle: `installer/test_bundle.bat`.
- [ ] Generar ZIP: `releases/ControlManager_vX.Y.Z.zip`.
- [ ] Compilar instalador EXE: `installer/build_installer_exe.bat`.
- [ ] Empaquetar asset instalador: `installer/package_installer_release.bat vX.Y.Z`.
- [ ] Crear tag Git: `vX.Y.Z`.
- [ ] Crear GitHub Release y subir ambos assets.

## Comandos sugeridos

```bash
build_all.bat
installer/populate_bundle.bat
installer/test_bundle.bat
```

```powershell
Compress-Archive -Path "bundle/ControlManager.bundle/*" -DestinationPath "releases/ControlManager_vX.Y.Z.zip" -Force
```

```bash
installer/build_installer_exe.bat
installer/package_installer_release.bat vX.Y.Z
```

## Publicacion en GitHub

1. Crear tag `vX.Y.Z`.
2. Crear nueva Release asociada al tag.
3. Adjuntar `ControlManager_vX.Y.Z.zip` como asset (bundle manual).
4. Adjuntar `ControlManager_Installer_vX.Y.Z.zip` como asset (instalacion automatica).
5. No subir ZIPs al arbol del repo; se mantienen fuera de git por `.gitignore`.

## Post-release y soporte de instalacion

- El ZIP publicado contiene el bundle (`ControlManager.bundle`), no scripts de instalacion.
- Si publicas el asset instalador, ahora contiene solo `ControlManager.Installer.exe` (single-file con bundle embebido).
- Ruta de instalacion recomendada para usuarios:
  `%APPDATA%\Autodesk\ApplicationPlugins\ControlManager.bundle`
- Verificar que exista:
  `%APPDATA%\Autodesk\ApplicationPlugins\ControlManager.bundle\PackageContents.xml`

### Incidencia conocida: `FileLoadException 0x80131515`

Si Windows bloquea DLLs descargadas de internet, pedir al usuario ejecutar:

```powershell
Get-ChildItem "$env:APPDATA\Autodesk\ApplicationPlugins\ControlManager.bundle" -Recurse -File | Unblock-File
```

Luego reiniciar Revit.

### Nota funcional de primer uso (Privacidad / EULA)

El dialogo de privacidad/aceptacion se muestra al primer lanzamiento del comando del plugin (al pulsar el boton), no durante el arranque de Revit.

## Uso del instalador single-file

- Un clic: `ControlManager.Installer.exe` (toggle: instala si no existe; si existe, pide confirmacion antes de desinstalar).
- Instalacion forzada: `ControlManager.Installer.exe --install`.
- Desinstalacion forzada: `ControlManager.Installer.exe --uninstall`.
- Tras exito, muestra un cuadro de dialogo de resultado (instalacion o desinstalacion completada), ademas de la salida por consola.
