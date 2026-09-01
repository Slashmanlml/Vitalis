# Vitalis

Sistema de gestion para consultorio medico virtual.

Vitalis es un proyecto de tesis para la carrera Analista de Sistemas. El objetivo es construir una plataforma web que permita administrar pacientes, turnos, historia clinica electronica, prescripciones, facturacion, sala de espera virtual, reportes y usuarios internos de un consultorio medico.

## Como levantarlo

### Con Docker (recomendado)

Es la forma mas rapida y la unica que no depende de que la maquina tenga
PostgreSQL, .NET y Node instalados. Requiere Docker Desktop.

```bash
docker compose up --build
```

Y despues abrir **http://localhost:8080**.

Se levantan tres contenedores: la base de datos, la API y el frontend. La API
aplica sus migraciones y siembra los datos de demostracion sola al arrancar. La
primera vez tarda varios minutos porque tiene que compilar todo; las siguientes
son cuestion de segundos.

Para apagarlo: `Ctrl+C`, o `docker compose down`. Para empezar de cero borrando
la base: `docker compose down -v`.

### Usuarios de demostracion

| Rol | Usuario | Contrasenia |
|---|---|---|
| Administrador | `admin@vitalis.local` | `Admin123!` |
| Medico (Laura Martinez, Pediatria) | `lmartinez@vitalis.local` | `Medico123!` |
| Recepcionista | `recepcion@vitalis.local` | `Recepcion123!` |
| Facturación | `facturacion@vitalis.local` | `Facturacion123!` |

### Sin Docker

Hacen falta .NET 8, Node 22 y PostgreSQL corriendo en local. En dos terminales:

```bash
# Terminal 1 - API en el puerto 5004
cd backend/src/Vitalis.Api
dotnet run

# Terminal 2 - frontend en el puerto 4200
cd vitalis-frontend
npm install
npx ng serve
```

La cadena de conexion se configura en `backend/src/Vitalis.Api/appsettings.json`.

### Pruebas

```bash
cd backend && dotnet test tests/Vitalis.Tests
cd vitalis-frontend && npx ng test --no-watch
```

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
- `vitalis-frontend/`: aplicacion web en Angular
- `database/`: scripts, modelo de datos y datos iniciales

## Estado

**Sistema funcional con los 10 modulos documentados**, incluyendo auditoria automatica,
bloqueos de agenda y busqueda global. JWT con autorizacion por rol en los 20 controladores
de la API, Swagger, PostgreSQL con migraciones EF Core y seed de datos de ejemplo.

Ver [docs/07-estado-actual-del-sistema.md](docs/07-estado-actual-del-sistema.md) para el
detalle de que esta implementado y que falta, y
[docs/05-backlog-mvp.md](docs/05-backlog-mvp.md) para el plan original del MVP.

Guía para ejecutar el proyecto: [docs/06-guia-inicio.md](docs/06-guia-inicio.md).

## Inicio rápido

1. Crear base `vitalis` en PostgreSQL.
2. Poner tu contraseña de `postgres` en `backend/src/Vitalis.Api/appsettings.Development.json`.
3. `dotnet run --project backend/src/Vitalis.Api`
4. `npm start` en `vitalis-frontend`
5. Login: `admin@vitalis.local` / `Admin123!`
