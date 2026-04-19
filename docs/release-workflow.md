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
- [ ] Crear tag Git: `vX.Y.Z`.
- [ ] Crear GitHub Release y subir el ZIP como asset.

## Comandos sugeridos

```bash
build_all.bat
installer/populate_bundle.bat
installer/test_bundle.bat
```

```powershell
Compress-Archive -Path "bundle/ControlManager.bundle/*" -DestinationPath "releases/ControlManager_vX.Y.Z.zip" -Force
```

## Publicacion en GitHub

1. Crear tag `vX.Y.Z`.
2. Crear nueva Release asociada al tag.
3. Adjuntar `ControlManager_vX.Y.Z.zip` como asset.
4. No subir ZIPs al arbol del repo; se mantienen fuera de git por `.gitignore`.
