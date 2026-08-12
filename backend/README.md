# Backend — Vitalis API

API REST en ASP.NET Core 8 con arquitectura en capas.

## Proyectos

| Proyecto | Responsabilidad |
|----------|-----------------|
| `Vitalis.Domain` | Entidades y constantes |
| `Vitalis.Application` | DTOs e interfaces de casos de uso |
| `Vitalis.Infrastructure` | EF Core, PostgreSQL, JWT, seed |
| `Vitalis.Api` | Controllers, Swagger, CORS |

## Ejecutar

```powershell
dotnet run --project src/Vitalis.Api
```

Configurá la conexión en `src/Vitalis.Api/appsettings.Development.json` (copiá desde `appsettings.Development.example.json`).

## Endpoints MVP v1

- `GET /api/health` — estado del sistema
- `POST /api/auth/login` — autenticación JWT

## Migraciones

```powershell
dotnet ef migrations add NombreMigracion `
  --project src/Vitalis.Infrastructure `
  --startup-project src/Vitalis.Api `
  --output-dir Migrations
```
