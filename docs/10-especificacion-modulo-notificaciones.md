# Especificación técnica — Módulo de Notificaciones por Correo

> **Documento de trabajo para la implementación.** Define *qué* hay que construir y
> *por qué*, con el detalle suficiente para que la implementación no tenga que
> tomar decisiones de arquitectura por su cuenta.
>
> Autor del diseño: Claude · Ejecución: Gemini · Proyecto: Vitalis (TFC)
> Fecha: 26/08/2026

---

## 1. Objetivo

Pasar de un **simulador** de correos a un **sistema real de notificaciones al
paciente**, con trazabilidad auditable.

Tres notificaciones nuevas, además de las que ya existen:

| # | Notificación | Cuándo se dispara |
|---|---|---|
| 1 | **Aviso de turno** | Al reservarse un turno y al confirmarse |
| 2 | **Recordatorio** | 24 h antes del turno, automáticamente |
| 3 | **Resumen de consulta** | Al registrarse la consulta médica (turno atendido) |

Las de cancelación y de nueva prescripción ya están implementadas y se conservan.

### Fuera de alcance

- SMS y WhatsApp.
- Preferencias de notificación por paciente (darse de baja). Queda anotado como
  trabajo futuro; mencionarlo en la tesina como limitación conocida suma más que
  omitirlo.

---

## 2. Decisión de proveedor

El sistema hoy no envía nada: `EmailService.SendEmailAsync` sólo escribe en la
tabla `EmailLogs`. Hay que elegir un proveedor real.

Comparación de planes gratuitos vigentes (verificado el 26/08/2026):

| Proveedor | Gratis | ¿Exige dominio propio? | Observación |
|---|---|---|---|
| **Brevo** | 300/día (~9.000/mes) | **No** | Relay SMTP + panel con estado de entrega |
| Gmail SMTP | 500/día | No | Requiere contraseña de aplicación; sin panel de entregas |
| Resend | 3.000/mes (100/día) | **Sí, en la práctica** | Sin dominio verificado sólo se envía a la propia casilla |
| Mailjet | 200/día | No | — |
| SendGrid | 100/día sólo 60 días | No | Se vuelve de pago; descartado |

**Decisión: Brevo, por SMTP.** Razones:

1. No exige dominio propio, que Tito no tiene. Resend queda descartado por eso:
   sin dominio verificado no se le puede escribir a la casilla de un paciente, que
   es justamente lo que hay que demostrar en la defensa.
2. Tiene panel de entregas, así que en la defensa se puede mostrar el correo
   llegando de verdad, no sólo el log interno.
3. 300/día sobra ampliamente.

**Importante — el código NO debe depender de Brevo.** Se programa contra SMTP
genérico (host, puerto, usuario, contraseña) para que cambiar a Gmail o a
cualquier otro sea sólo tocar configuración. Eso es defendible ante el jurado:
*"la aplicación depende de un protocolo, no de un proveedor"*.

**Librería: MailKit** (paquete NuGet `MailKit`). `System.Net.Mail.SmtpClient` está
marcado como obsoleto por Microsoft y no debe usarse.

> **Sobre las credenciales:** la clave SMTP la genera y la carga Tito. Va en
> *user-secrets* (`dotnet user-secrets set`), **nunca** en `appsettings.json`
> versionado. Ya hay un antecedente en el repo: `appsettings.Development.json`
> tiene la contraseña de PostgreSQL en texto plano y está en `.gitignore` — el
> mismo criterio aplica acá, pero user-secrets es más prolijo.

---

## 3. Configuración

Agregar a `appsettings.json` (con valores vacíos; los reales van en user-secrets):

```jsonc
"Notificaciones": {
  "Habilitado": true,
  "Host": "smtp-relay.brevo.com",
  "Puerto": 587,
  "Usuario": "",
  "Password": "",
  "RemitenteNombre": "Vitalis",
  "RemitenteEmail": "no-responder@vitalis.local",
  "ModoPrueba": false,
  "RedirigirTodoA": "",
  "HorasAntesDelRecordatorio": 24,
  "MinutosEntreBarridos": 30
}
```

Dos campos merecen explicación, porque son los que evitan un accidente en la demo:

- **`ModoPrueba`**: en `true` no se envía nada; se registra en `EmailLogs` con
  `Estado = "Simulado"`. Permite desarrollar y demostrar el flujo completo sin
  gastar cuota ni escribirle a nadie.
- **`RedirigirTodoA`**: si tiene un valor, **todos** los correos van a esa casilla
  sin importar el destinatario real (el destinatario original se conserva en el
  log y se antepone al asunto). Es la red de seguridad para que los datos de
  prueba de `DbSeeder` no le escriban a direcciones inventadas o, peor, reales.

Crear una clase de opciones `NotificacionesOptions` y registrarla con el patrón
`IOptions<T>` en `DependencyInjection.cs`. No leer configuración con
`IConfiguration["..."]` suelto dentro del servicio.

---

## 4. Cambios en el modelo de datos

### 4.1 Problema actual

`EmailLog` tiene sólo `Id, Destinatario, Asunto, Cuerpo, FechaEnvio`.

Eso hace que **un correo simulado a mano desde la pantalla sea indistinguible de
una notificación real emitida por el sistema**, porque van a la misma tabla sin
ningún campo que los separe. Si en la defensa preguntan *"¿cómo sabés que esta
notificación se envió?"*, hoy no hay respuesta. Un registro que cualquiera puede
fabricar y borrar no es un registro de auditoría.

### 4.2 Entidad nueva

```csharp
namespace Vitalis.Domain.Entities;

public class EmailLog
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

    // --- campos nuevos ---

    /// <summary>"Sistema" si lo emitió un evento de negocio; "Simulado" si lo
    /// generó un administrador desde la pantalla. Es lo que separa la evidencia
    /// real de la fabricada.</summary>
    public string Origen { get; set; } = OrigenNotificacion.Sistema;

    /// <summary>Qué lo disparó. Ver la clase EventoNotificacion.</summary>
    public string Evento { get; set; } = EventoNotificacion.Personalizado;

    /// <summary>Turno que originó la notificación, cuando aplica. Permite
    /// responder "¿qué se le notificó a este paciente sobre este turno?".</summary>
    public int? TurnoId { get; set; }
    public Turno? Turno { get; set; }

    /// <summary>"Enviado" | "Fallido" | "Simulado".</summary>
    public string Estado { get; set; } = EstadoNotificacion.Enviado;

    /// <summary>Detalle del error cuando Estado = "Fallido".</summary>
    public string? MensajeError { get; set; }
}
```

Y las constantes, en `Vitalis.Domain/Constants/` junto a `Roles.cs` (no usar
literales sueltos en el código):

```csharp
public static class OrigenNotificacion
{
    public const string Sistema  = "Sistema";
    public const string Simulado = "Simulado";
}

public static class EventoNotificacion
{
    public const string TurnoCreado       = "TurnoCreado";
    public const string TurnoConfirmado   = "TurnoConfirmado";
    public const string TurnoCancelado    = "TurnoCancelado";
    public const string RecordatorioTurno = "RecordatorioTurno";
    public const string ResumenConsulta   = "ResumenConsulta";
    public const string NuevaPrescripcion = "NuevaPrescripcion";
    public const string BienvenidaPaciente = "BienvenidaPaciente";
    public const string Personalizado     = "Personalizado";
}

public static class EstadoNotificacion
{
    public const string Enviado  = "Enviado";
    public const string Fallido  = "Fallido";
    public const string Simulado = "Simulado";
}
```

### 4.3 Configuración EF y migración

En `VitalisDbContext.OnModelCreating`, para `EmailLog`:

- `Origen`, `Evento`, `Estado`: `HasMaxLength(40)`, requeridos.
- Relación con `Turno`: **`OnDelete(DeleteBehavior.SetNull)`**. Es deliberado —
  si algún día se borra un turno, el registro de auditoría debe sobrevivir con
  `TurnoId = null`; borrar en cascada la evidencia sería exactamente lo contrario
  de lo que se busca.
- Índice sobre `(TurnoId, Evento)`: es la consulta que hace el barrido de
  recordatorios en cada corrida, y sin índice hace un scan completo.

Comando de migración (lo corre Tito):

```powershell
cd "C:\Users\Tito\Desktop\New project\backend"
dotnet ef migrations add EnriquecerEmailLogParaAuditoria --project src\Vitalis.Infrastructure --startup-project src\Vitalis.Api
```

> Los registros que ya existen quedan con los valores por defecto
> (`Origen = "Sistema"`, `Evento = "Personalizado"`, `Estado = "Enviado"`), que es
> razonable: son los correos que el sistema ya había emitido.

### 4.4 Restringir el borrado

`LimpiarLogsAsync` borra toda la tabla de un botón. **Eliminar ese método, su
endpoint `DELETE /api/EmailLogs/limpiar` y el botón del frontend.** Un registro de
auditoría que se puede vaciar no prueba nada.

`EliminarLogAsync` (borrado individual) se conserva **sólo para `Origen =
"Simulado"`**: si el registro es del sistema, devolver `ConflictException` con el
mensaje *"No se pueden eliminar notificaciones emitidas por el sistema."* Esa
regla es un buen material de defensa.

---

## 5. Contrato del servicio

```csharp
public interface IEmailService
{
    /// <summary>Envía y registra. Nunca lanza: ante una falla registra
    /// Estado="Fallido" y devuelve false.</summary>
    Task<bool> NotificarAsync(NotificacionRequest request);

    Task<IEnumerable<EmailLog>> GetEmailLogsAsync(string? origen = null, string? evento = null);

    /// <summary>Alta manual desde la pantalla. Fuerza Origen="Simulado".</summary>
    Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion,
                                     string? asuntoPersonalizado = null,
                                     string? cuerpoPersonalizado = null);

    Task<bool> EliminarLogAsync(int id);
}

public class NotificacionRequest
{
    public string Destinatario { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public int? TurnoId { get; set; }
    /// <summary>Datos para la plantilla: nombre del paciente, fecha del turno,
    /// profesional, etc.</summary>
    public Dictionary<string, string> Datos { get; set; } = new();
}
```

`SendEmailAsync(to, subject, body)` se elimina del contrato: hoy permite emitir
una notificación sin declarar qué evento la originó, que es la raíz del problema
de auditoría. Todos los llamadores pasan a `NotificarAsync`.

### 5.1 Regla de oro: notificar nunca rompe la operación

Hoy `SendEmailAsync` sólo escribe en la base, así que no puede fallar. **Cuando
empiece a hablar SMTP de verdad, sí va a fallar** — servidor caído, credencial
vencida, cuota agotada.

Si eso propaga una excepción, **crear un turno va a devolver 500 aunque el turno
se haya guardado bien**. Es el riesgo más serio de todo este cambio.

Por lo tanto:

- `NotificarAsync` captura toda excepción, registra `Estado = "Fallido"` con
  `MensajeError`, y devuelve `false`.
- Los llamadores **no** envuelven la llamada en `try/catch` ni miran el resultado
  para decidir si siguen.
- El envío ocurre **después** del `SaveChangesAsync()` de la operación de negocio,
  nunca dentro de la misma transacción.

---

## 6. Plantillas

Gemini ya escribió plantillas HTML con la paleta teal correcta (`#0f766e`).
**Conservarlas**, pero moverlas fuera del cuerpo del método:

- Crear `Vitalis.Infrastructure/Notificaciones/PlantillasEmail.cs` con un método
  por evento que reciba el diccionario `Datos` y devuelva `(asunto, cuerpo)`.
- Reemplazar los textos genéricos por los datos reales. Hoy la plantilla de
  confirmación dice *"Fecha estimada: Próxima cita programada"*, que no informa
  nada. Debe decir la fecha, la hora y el profesional concretos.

Marcadores mínimos por plantilla:

| Evento | Datos requeridos |
|---|---|
| TurnoCreado / TurnoConfirmado | `PacienteNombre`, `FechaHora`, `ProfesionalNombre`, `Especialidad` |
| RecordatorioTurno | idem + `HorasRestantes` |
| TurnoCancelado | `PacienteNombre`, `FechaHora`, `ProfesionalNombre` |
| ResumenConsulta | `PacienteNombre`, `Fecha`, `ProfesionalNombre`, `Diagnostico`, `Indicaciones` |
| NuevaPrescripcion | `PacienteNombre`, `ProfesionalNombre`, `CantidadMedicamentos` |

Formato de fecha en los correos: `dd/MM/yyyy HH:mm` (formato argentino), no ISO.

> **Cuidado con el resumen de consulta:** lleva diagnóstico e indicaciones, o sea
> datos clínicos sensibles, a una casilla de correo. Para la tesina conviene
> **incluir sólo fecha, profesional e indicaciones generales, y omitir el
> diagnóstico**, con una línea en el documento explicando la decisión. Que aparezca
> el criterio de confidencialidad vale más que el campo de más.

---

## 7. Dónde se dispara cada evento

| Evento | Archivo | Punto exacto |
|---|---|---|
| `TurnoCreado` | `TurnoService.CrearAsync` | Después del `SaveChangesAsync` |
| `TurnoConfirmado` | `TurnoService.EditarAsync` | Sólo cuando `Confirmado` pasa de `false` a `true` |
| `TurnoCancelado` | `TurnoService.EditarAsync` | Ya existe; adaptarlo al contrato nuevo |
| `ResumenConsulta` | `ConsultaMedicaService.CrearAsync` | Después de marcar el turno como "Atendido" |
| `NuevaPrescripcion` | `PrescripcionService.CrearAsync` | Ya existe; adaptarlo |
| Cancelación en cascada | `BloqueoAgendaService` (línea ~141) | Ya existe; adaptarlo |
| `RecordatorioTurno` | `RecordatorioTurnosService` | Ver sección 8 |

**Detalle importante:** `TurnoConfirmado` sólo se emite en la *transición*. Si se
edita un turno ya confirmado (por ejemplo para cambiarle la obra social), no debe
mandarse otro correo de confirmación. `EditarAsync` ya usa este patrón para la
cancelación (`estadoAnterior != "Cancelado"`); replicarlo.

### 7.1 Minas conocidas en el código actual

Tres cosas que hay que corregir **sí o sí** al hacer este cambio, porque hoy son
inofensivas y dejan de serlo en cuanto el envío sea real:

**(a) El destinatario de relleno.** Hay **seis** llamadas con esta forma:

```csharp
await _emailService.SendEmailAsync(pac.Email ?? "paciente@vitalis.local", asunto, cuerpo);
```

`TurnoService` líneas ~172, 224, 239, 257, 282 y `PrescripcionService` línea ~165.

Mientras el envío es simulado no pasa nada. Con SMTP real, **el sistema le va a
escribir a un dominio que no existe** cada vez que un paciente no tenga email
cargado: rebotes, cuota quemada y, si se acumulan, reputación de remitente
arruinada.

Reemplazar las seis por la regla: **si el paciente no tiene email, no se intenta
enviar y no se registra nada.** Nunca inventar un destinatario.

**(b) `IEmailService` inyectado como opcional.** `PrescripcionService` lo declara
`private readonly IEmailService? _emailService;` y después pregunta
`if (_emailService != null)`. Es una dependencia obligatoria disfrazada de
opcional: si un día no se registra en el contenedor, las notificaciones
desaparecen en silencio en vez de fallar al arrancar. Cambiarlo a
`IEmailService` no nulo, como en `TurnoService` y `BloqueoAgendaService`.

**(c) `Turno.Paciente` y `Turno.Profesional` son navegaciones anulables**
(`Paciente?`, `Profesional?`). En la consulta del barrido de recordatorios eso
genera advertencias `CS8602`; escribir la condición completa
(`t.Paciente != null && t.Paciente.Email != null`) en vez de silenciarla con `!`.
El proyecto ya arrastra ~19 advertencias de este tipo; no agregar más.

---

## 8. Servicio de recordatorios

Es la parte más delicada del módulo, porque es la única que corre sola.

### 8.1 Forma

`RecordatorioTurnosService : BackgroundService`, en
`Vitalis.Infrastructure/Notificaciones/`, registrado con `AddHostedService`.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            await ProcesarRecordatoriosAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            // Nunca dejar morir el bucle: si una corrida falla, se loguea y se
            // reintenta en el próximo barrido. Una excepción sin capturar acá
            // apaga el servicio en silencio para el resto de la vida del proceso.
            _logger.LogError(ex, "Falló el barrido de recordatorios");
        }

        await Task.Delay(TimeSpan.FromMinutes(_opciones.MinutosEntreBarridos), stoppingToken);
    }
}
```

### 8.2 Alcance del DbContext

`BackgroundService` es *singleton* y `VitalisDbContext` está registrado como
*scoped*. **Inyectar el DbContext directamente en el constructor no compila
correctamente en tiempo de ejecución** (o peor: comparte un contexto entre
barridos y acumula entidades rastreadas indefinidamente).

Hay que inyectar `IServiceScopeFactory` y abrir un alcance por barrido:

```csharp
using var scope = _scopeFactory.CreateScope();
var contexto = scope.ServiceProvider.GetRequiredService<VitalisDbContext>();
var notificador = scope.ServiceProvider.GetRequiredService<IEmailService>();
```

### 8.3 Selección de turnos e idempotencia

```csharp
var ahora = DateTime.UtcNow;
var desde = ahora.AddHours(_opciones.HorasAntesDelRecordatorio);
var hasta = desde.AddMinutes(_opciones.MinutosEntreBarridos);

var candidatos = await contexto.Turnos
    .Include(t => t.Paciente)
    .Include(t => t.Profesional).ThenInclude(p => p!.Especialidad)
    .Where(t => t.FechaHora >= desde
             && t.FechaHora <  hasta
             && t.Estado != "Cancelado"
             && t.Paciente.Email != null
             && !contexto.EmailLogs.Any(l => l.TurnoId == t.Id
                                          && l.Evento == EventoNotificacion.RecordatorioTurno
                                          && l.Estado == EstadoNotificacion.Enviado))
    .ToListAsync(ct);
```

Tres decisiones que hay que respetar tal cual:

1. **La ventana es `[desde, hasta)`, del ancho de un barrido.** Si en vez de eso se
   usara "todos los turnos de las próximas 24 h", cada corrida volvería a tomar
   los mismos turnos.
2. **La idempotencia sale de consultar `EmailLogs`, no de un flag en `Turno`.** No
   agregar una columna `RecordatorioEnviado`: el log ya es la fuente de verdad y
   un flag aparte se desincroniza.
3. **Sólo cuentan los `Enviado`.** Si un recordatorio falló, el próximo barrido
   debe reintentarlo; por eso no alcanza con que exista el registro.

### 8.4 Riesgo de borde a tener en cuenta

Si la API está apagada durante la ventana de un turno, ese recordatorio **no se
manda nunca**. Es aceptable para un TFC, pero hay que **documentarlo como
limitación conocida** en la tesina. La solución industrial sería una ventana de
gracia con marca de "último barrido"; mencionarlo demuestra que se entiende el
problema aunque no se lo resuelva.

---

## 9. Frontend

Renombrar la sección: **"Simulación de Mails" → "Notificaciones"**. Ruta
`/dashboard/mails-simulados` → `/dashboard/notificaciones` (actualizar
`app.config.ts` y el enlace del menú en `dashboard.html`).

La pantalla debe mostrar, por cada registro:

- Una **insignia de origen**: *Sistema* (teal, `var(--color-primary)`) vs
  *Simulado* (gris, `var(--text-muted)`). Es lo primero que tiene que ver el ojo.
- Una **insignia de estado**: Enviado / Fallido / Simulado, con los colores
  semánticos ya existentes.
- El **evento** en texto legible ("Recordatorio de turno", no "RecordatorioTurno").
- Filtros por origen, por evento y por estado.
- Al expandir un registro fallido, el `MensajeError`.

Reglas de estilo, no negociables porque el resto del sistema ya las cumple:

- **Cero colores hardcodeados.** Todo sale de los tokens de `styles.css`
  (`--color-primary`, `--text-secondary`, `--border-color`, etc.). En la ronda
  anterior se barrieron 403 valores pegados a mano justamente para esto.
- El estado **nunca se comunica sólo por color**: siempre insignia con texto.
- Quitar el botón de "limpiar todo".

---

## 10. Tests exigidos

En `backend/tests/Vitalis.Tests/`. El backend viene de 73 en verde; esto no puede
bajar ese número.

**`EmailServiceTests`** (adaptar los existentes al contrato nuevo):

1. `NotificarAsync` registra con `Origen = "Sistema"` y el `Evento` recibido.
2. `NotificarAsync` con `ModoPrueba = true` registra `Estado = "Simulado"` y **no**
   invoca al cliente SMTP.
3. `NotificarAsync` **no lanza** cuando el envío falla, y deja `Estado = "Fallido"`
   con el `MensajeError` cargado.
4. `RedirigirTodoA` cambia el destinatario efectivo pero conserva el original en
   el log.
5. `SimularEnvioAsync` siempre fuerza `Origen = "Simulado"`.
6. `EliminarLogAsync` rechaza con `ConflictException` un registro de origen
   `"Sistema"`.

**`RecordatorioTurnosServiceTests`** (los más importantes):

7. Toma un turno que cae dentro de la ventana.
8. **No** toma uno fuera de la ventana.
9. **No** toma uno cancelado.
10. **No** vuelve a tomar uno que ya tiene un recordatorio `Enviado` *(prueba de
    idempotencia: correr el barrido dos veces seguidas debe producir un solo
    correo)*.
11. **Sí** vuelve a tomar uno cuyo recordatorio anterior quedó `Fallido`.
12. No toma un turno cuyo paciente no tiene email.

**`TurnoServiceTests`** (agregar):

13. Crear un turno genera un `EmailLog` con `Evento = "TurnoCreado"`.
14. Confirmar un turno ya confirmado **no** genera un segundo correo.
15. Si el envío falla, **el turno igual se crea** y la operación devuelve éxito
    *(esta es la prueba que protege contra el riesgo de la sección 5.1)*.

Para todo esto hace falta un doble de prueba del cliente SMTP: extraer una
interfaz `IClienteSmtp` con un solo método `EnviarAsync`, e implementar un
`ClienteSmtpFalso` que registre las llamadas y pueda configurarse para lanzar
excepción. Sin esa costura, estos tests no se pueden escribir.

---

## 11. Checklist de aceptación

Antes de dar el módulo por terminado:

- [ ] `dotnet test tests\Vitalis.Tests` en verde, con **al menos 88** pruebas
      (73 actuales + 15 nuevas).
- [ ] `ng build` sin errores y `ng test` en verde.
- [ ] No queda ninguna referencia a `SendEmailAsync` en el código.
- [ ] `grep` de colores hexadecimales en el CSS de la pantalla nueva: cero
      resultados.
- [ ] Con `ModoPrueba = true` se puede recorrer el flujo completo sin enviar nada.
- [ ] Con credenciales reales llega un correo de verdad a una casilla propia.
- [ ] La pantalla distingue a simple vista un correo del sistema de uno simulado.
- [ ] `appsettings.json` versionado **no** contiene la clave SMTP.
- [ ] `grep -rn "paciente@vitalis.local" backend/` devuelve **cero** resultados.

---

## 12. Orden sugerido de implementación

Cada paso deja el sistema compilando y con los tests en verde:

1. Entidad `EmailLog` + constantes + configuración EF + migración.
2. `NotificacionesOptions` + `IClienteSmtp` + implementación con MailKit.
3. `IEmailService` nuevo contrato + `NotificarAsync` + tolerancia a fallas.
4. Mover las plantillas a `PlantillasEmail.cs` y completarlas con datos reales.
5. Cablear los eventos en `TurnoService` y `ConsultaMedicaService`.
6. `RecordatorioTurnosService` + registro en el contenedor.
7. Tests (secciones 10.1 a 10.15).
8. Frontend: renombrar, insignias, filtros, sacar el "limpiar todo".

**Los pasos 1 a 3 son los que no se pueden hacer mal.** Si algo queda a medias,
que sea del 6 en adelante: un módulo con auditoría correcta y sin recordatorios
automáticos es defendible; uno con recordatorios que rompen el alta de turnos, no.
