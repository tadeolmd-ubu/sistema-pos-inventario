# Reglas de colaboración

## Capas

- `Models`: entidades y enums. No depende de nada.
- `Data`: `AppDbContext` y mapeo. No contiene reglas de negocio.
- `Service`: validación, normalización y persistencia. Devuelve `Resultado`.
- `ViewModels`: estado, comandos y orquestación. No abre ventanas.
- `Views`: XAML y code-behind mínimo. No llama servicios.

## Ramas

- `feat/services/<modulo>`
- `feat/viewmodels/<modulo>`
- `feat/views/<modulo>`
- `fix/<modulo>`

## Antes de un PR

```bash
dotnet build PosInventario.sln
dotnet test PosInventario.sln
git diff --check
```

## Prohibido

- Crear repositorios o Unit of Work.
- Usar `Remove()` en entidades de negocio.
- Poner lógica de negocio en XAML o en el code-behind.
- Versionar archivos `.db`.
