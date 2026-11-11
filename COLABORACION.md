# Reglas de colaboración

## Capas

- `Models`: entidades y enums. No depende de nada.
- `Data`: `AppDbContext` y mapeo. No contiene reglas de negocio.
- `Service`: validación, normalización y persistencia. Devuelve `Resultado`.
- `ViewModels`: estado, comandos y orquestación. No abre ventanas.
- `Views`: XAML y code-behind mínimo. No llama servicios.

## Ramas

- `dev`: rama de integración donde se fusiona todo el trabajo.
- `main`: solo recibe fusiones estables; cada push genera la release
  (`deploy.yml`).
- Ramas de trabajo desde `dev`:
  - `feat/<modulo>` (p. ej. `feat/caja`, `feat/historial-ventas`)
  - `fix/<modulo>` (p. ej. `fix/errores-globales`)
  - `chore/<tema>` (p. ej. `chore/textos-espanol-mexico`)

## Antes de fusionar a `dev`

```bash
dotnet build PosInventario.sln
dotnet test PosInventario.sln
git diff --check
```

## Al cerrar un ítem de `PENDIENTES.md`

Cambia `- [ ]` por `- [x]` y agrega el hash del commit en el propio documento.

## Prohibido

- Crear repositorios o Unit of Work.
- Usar `Remove()` en entidades de negocio.
- Poner lógica de negocio en XAML o en el code-behind.
- Versionar archivos `.db` ni respaldos.
