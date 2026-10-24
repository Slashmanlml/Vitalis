# Registro de pacientes, confirmación por correo y auditoría de seguridad

> Octubre 2026 · rama `feature/registro-confirmacion-auditoria`.
> El informe completo de la auditoría (matriz de hallazgos, detalle, riesgos
> residuales y verificación) está en el documento "Vitalis — Informe de
> Auditoría de Seguridad". Este archivo resume lo que hace falta para usar y
> probar lo nuevo.

## Qué se agregó

| Pantalla | Ruta | Quién |
| --- | --- | --- |
| Registrarme | `/registro` | Público |
| Confirmar correo (destino del enlace del mail) | `/confirmar-email?token=...` | Público |
| Portal del paciente: mis datos y mis turnos | `/portal` | Rol `Paciente` |
| Olvidé mi contraseña | `/olvide-password` | Público |
| Elegir contraseña nueva (destino del enlace del mail) | `/restablecer-password?token=...` | Público |

| Endpoint | Para qué |
| --- | --- |
| `POST /api/auth/registro` | Crea la cuenta sin confirmar y manda el mail. Siempre 202 con mensaje genérico. |
| `POST /api/auth/confirmar-email` | Valida el token del enlace y activa la cuenta. |
| `POST /api/auth/reenviar-confirmacion` | Manda un enlace nuevo (espera mínima 60 s). |
| `POST /api/auth/olvide-password` | Manda el enlace para cambiar la contraseña. Siempre 202 con el mismo mensaje. |
| `POST /api/auth/restablecer-password` | Con el token del enlace, guarda la contraseña nueva y avisa por mail. |
| `GET /api/portal/mi-perfil`, `GET /api/portal/mis-turnos` | Datos del paciente del token. No reciben ids. |

Base de datos (migración `RegistroPacientesYSeguridad`): columnas nuevas en
`usuarios` (`EmailConfirmado`, `FechaConfirmacionEmail`, `IntentosFallidos`,
`BloqueadoHasta`, `UltimoAcceso`), `pacientes.UsuarioId` (único) y la tabla
`tokens_verificacion` (solo guarda el SHA-256 del token).

## Cómo probarlo en la máquina de desarrollo

1. La API necesita `Jwt:Key` en `backend/src/Vitalis.Api/appsettings.Development.json`
   (ver `backend/README.md`). Sin clave no arranca, a propósito.
2. Levantar backend y frontend como siempre. La migración se aplica sola al arrancar.
3. Entrar a `http://localhost:4200/registro` y crear una cuenta.
4. Con `Notificaciones:ModoPrueba = true` el mail no sale: entrar como
   `admin@vitalis.local`, ir a **Notificaciones**, abrir el correo
   "Confirmá tu correo..." y hacer clic en **Confirmar mi correo**.
5. Volver al login con la cuenta nueva: entra directo al portal.
6. Recuperación: en el login, **¿Te la olvidaste?** → escribir el correo →
   en **Notificaciones** abrir "Elegí una contraseña nueva" → **Elegir una
   contraseña nueva**. Llega además el aviso "Tu contraseña fue cambiada".
7. En modo demo, al arrancar la API se arma la **jornada de hoy** (13 turnos
   en distintos estados según la hora) si todavía no hay turnos para hoy.

Para que el mail llegue de verdad: `ModoPrueba = false` y datos SMTP en la
sección `Notificaciones` (Brevo gratuito, o Gmail con contraseña de aplicación).
En ese modo el enlace se oculta en el historial de Notificaciones.

## Reglas de seguridad que conviene saber explicar

- Contraseñas: 8+ caracteres, mayúscula, minúscula y número (registro, alta y cambio).
- 5 contraseñas incorrectas seguidas bloquean la cuenta 15 minutos.
- El enlace de recuperación vence en 60 minutos, sirve una vez y pedir otro
  anula el anterior. Solo se manda a cuentas activas y con correo confirmado.
  Al usarlo se desbloquea la cuenta y se avisa por mail del cambio.
- Los tokens de confirmación y de recuperación no son intercambiables (cada
  uno tiene su propósito guardado junto al hash).
- 10 logins por minuto y 10 registros cada 10 minutos por IP; después, 429.
- Si el DNI ya tiene ficha con otro correo, no se vincula: hay que pasar por recepción.
- Los usuarios de prueba solo existen con `Semilla:DatosDemo = true`
  (por defecto en Development y en el `docker-compose.yml` de la demo).
- Docker exige `JWT_KEY` en un archivo `.env` (copiar `.env.example`).
