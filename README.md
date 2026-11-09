# Sistema de inventario

Aplicación de escritorio construida con Avalonia, MVVM (CommunityToolkit),
EF Core y SQLite. El modelo de datos está mapeado y la migración base creada;
los servicios y las pantallas se implementan por fases.

## Requisitos

- .NET 10 SDK
- Windows x64 para publicar (desarrollo posible en Linux/macOS)

## Desarrollo

```bash
dotnet restore
dotnet build PosInventario.sln
dotnet test PosInventario.sln
dotnet run --project PosInventario/PosInventario.csproj
```

## Base de datos

SQLite local (`SistemaInventario.db`). Las migraciones se aplican al iniciar.

- En `Debug` la base se busca en el directorio actual y sus padres.
- En `Release` se usa `%LOCALAPPDATA%/SistemaInventario/SistemaInventario.db`
  (o `~/.local/share/SistemaInventario` en Linux) y se importa una base
  portable si existe junto al ejecutable.

Crear/actualizar la base en desarrollo:

```bash
cd PosInventario
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Nombre>
dotnet tool run dotnet-ef database update
```

## Publicación

El workflow `.github/workflows/deploy.yml` ejecuta las pruebas, publica
`win-x64` self-contained y crea una release en cada push a `main`.

## Estructura

```text
PosInventario/          aplicación (Models, Data, Service, ViewModels, Views, Styles, Controls)
PosInventario.Tests/    pruebas xUnit sobre SQLite en memoria y en disco
```

Reglas de capas y convenciones: ver `ARQUITECTURA-BASE.md` y `COLABORACION.md`.
