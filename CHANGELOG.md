# Changelog

## [v1.0.2] - 2026-09-04

### UI / ribbon

- Icono de aplicacion en la ventana principal y en el dialogo de privacidad.
- Panel del ribbon renombrado a **Quality Control**.

## [v1.0.1] - 2026-08-01

### App Store / seguridad

- Actualiza `System.IO.Packaging` de 8.0.0 a 10.0.10 (vulnerabilidad detectada por Autodesk).
- Empaqueta DLLs para Revit 2023-2027 en el bundle.
- Corrige runtime: 2023-2024 (.NET Framework 4.8), 2025-2026 (.NET 8), 2027 (.NET 10).
- `ElementId`: usa `Value` (long) desde Revit 2024+ (requerido en 2026+).

## [v1.0.0] - 2026-04-16

### Primera version

- Deteccion de 5 tipos de problemas BIM.
- Exportacion a Excel con ClosedXML.
- Seleccion de elementos directamente en Revit.
- Compatible con Revit 2023-2027 (dual framework: .NET 4.8 + .NET 10).
- Estructura bundle lista para Autodesk App Store.
