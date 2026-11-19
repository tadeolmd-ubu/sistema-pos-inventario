# Reglas de colaboración

## Repositorio

- Remoto oficial: `git@github.com:tadeolmd-ubu/sistema-pos-inventario.git`.
- `dev` es la rama de integración; `main` publica releases mediante
  `.github/workflows/deploy.yml`.
- No se versionan bases `.db`, respaldos ni archivos de `bin/` u `obj/`.

## Módulos

- Operación: Inicio, Caja, Ventas, Devoluciones y movimientos de efectivo.
- Catálogos: Productos, Proveedores y Clientes.
- Inventario: Inventario y Compras.
- Administración: Reportes, Usuarios y Configuración.
- Soporte: Login, Setup, respaldos, restauración, ticket y corte de caja.

Cada módulo sigue el patrón `View` → `ViewModel` → `Service`; la composición
manual de dependencias vive en `App.axaml.cs`.

## Operación y diagnóstico

- **Instancia única:** `InstanciaUnica` usa un bloqueo de archivo en
  `LocalApplicationData/SistemaInventario/instancia.lock`. Al intentar abrir
  una segunda instancia se muestra un aviso y se cierra.
- **Logs:** `RegistroApp` escribe en
  `LocalApplicationData/SistemaInventario/logs/app-YYYYMMDD.log`, con retención
  de 30 días. Nunca lanza excepciones.
- **Base de datos:** `SistemaInventario.db`; en `Release` vive en
  `LocalApplicationData/SistemaInventario`. Las migraciones se aplican al
  iniciar y antes se crea un respaldo.
- **Respaldos y restauración:** se validan con `PRAGMA integrity_check` y las
  tablas mínimas del sistema.

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
cd PosInventario
dotnet tool restore
dotnet tool run dotnet-ef migrations has-pending-model-changes
cd ..
git diff --check
```

## Al cerrar un ítem de `PENDIENTES.md`

Cambia `- [ ]` por `- [x]` y agrega el hash del commit en el propio documento.

## Prohibido

- Crear repositorios o Unit of Work.
- Usar `Remove()` en entidades de negocio.
- Poner lógica de negocio en XAML o en el code-behind.
- Versionar archivos `.db` ni respaldos.
