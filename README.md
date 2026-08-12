# Vitalis

Sistema de gestion para consultorio medico virtual.

Vitalis es un proyecto de tesis para la carrera Analista de Sistemas. El objetivo es construir una plataforma web que permita administrar pacientes, turnos, historia clinica electronica, prescripciones, facturacion, sala de espera virtual, reportes y usuarios internos de un consultorio medico.

## Stack tecnologico

- Frontend: Angular
- Backend: ASP.NET Core / .NET 8 LTS
- Base de datos: PostgreSQL
- ORM: Entity Framework Core
- Autenticacion: JWT con roles y permisos
- Documentacion de API: Swagger / OpenAPI

## Modulos principales

- Seguridad, usuarios, roles y permisos
- Gestion de pacientes
- Historia clinica electronica
- Agenda y turnos
- Sala de espera virtual
- Prescripciones y recetas
- Facturacion y liquidacion
- Reportes y estadisticas
- Administracion general
- Notificaciones internas

## Estructura inicial

- `docs/`: documentacion funcional y tecnica de la tesis
- `backend/`: API del sistema en ASP.NET Core
- `frontend/`: aplicacion web en Angular
- `database/`: scripts, modelo de datos y datos iniciales

## Estado

**MVP v1 en progreso:** backend con JWT y Swagger, frontend con login, PostgreSQL y seed de roles/usuario admin.

Guía para ejecutar el proyecto: [docs/06-guia-inicio.md](docs/06-guia-inicio.md).

## Inicio rápido

1. Crear base `vitalis` en PostgreSQL.
2. Poner tu contraseña de `postgres` en `backend/src/Vitalis.Api/appsettings.Development.json`.
3. `dotnet run --project backend/src/Vitalis.Api`
4. `npm start` en `frontend/vitalis-web`
5. Login: `admin@vitalis.local` / `Admin123!`
