# Guía de inicio — Vitalis MVP v1

## Requisitos

- .NET 8 SDK
- Node.js 20+ y Angular CLI
- PostgreSQL 17

## 1. Base de datos

Creá la base `vitalis` (una sola vez):

```powershell
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -U postgres -c "CREATE DATABASE vitalis;"
```

## 2. Configurar contraseña de PostgreSQL

Editá `backend/src/Vitalis.Api/appsettings.Development.json` y reemplazá `CAMBIAR` por tu contraseña de `postgres`.

## 3. Backend (API)

```powershell
cd backend
dotnet run --project src/Vitalis.Api
```

- Swagger: https://localhost:7299/swagger
- Health: https://localhost:7299/api/health
- Login: `POST /api/auth/login`

Al iniciar, la API aplica migraciones y crea roles + usuario admin.

**Credenciales demo:** `admin@vitalis.local` / `Admin123!`

## 4. Frontend

```powershell
cd frontend/vitalis-web
npm start
```

Abrí http://localhost:4200 e iniciá sesión con el usuario demo.

## 5. Certificado HTTPS (si el navegador bloquea la API)

```powershell
dotnet dev-certs https --trust
```

## Estructura creada

```text
backend/
  Vitalis.sln
  src/
    Vitalis.Domain/
    Vitalis.Application/
    Vitalis.Infrastructure/
    Vitalis.Api/
frontend/
  vitalis-web/
database/
  scripts/
  seed/
```
