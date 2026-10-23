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

**Clave JWT (obligatoria).** La API no arranca sin `Jwt:Key` (mínimo 32 caracteres). No va en
`appsettings.json` porque ese archivo se sube a git. Generá una clave aleatoria y ponela en
`appsettings.Development.json` (que no se sube), por ejemplo desde PowerShell:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
```

**Correo de confirmación.** Con `Notificaciones:ModoPrueba = true` los correos no salen: quedan en
la pantalla *Notificaciones* (Administrador), donde se puede abrir el enlace de confirmación. Para
mandarlos de verdad, poné `ModoPrueba = false` y los datos SMTP (por ejemplo, una cuenta gratuita de
Brevo o una contraseña de aplicación de Gmail) en `Notificaciones` de `appsettings.Development.json`.

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
