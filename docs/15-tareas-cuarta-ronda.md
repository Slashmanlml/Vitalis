# Cuarta ronda de tareas

> Buscá tu nombre. Tenés tu propia lista de archivos y **nadie más los toca**.
>
> Antes de escribir una línea, leé `AGENTS.md` en la raíz del repositorio: ahí
> están las reglas de estilo que no se negocian y la lista de archivos
> compartidos que no se tocan.

**Estado al arrancar esta ronda:** 112 pruebas de backend y 37 de frontend, todas
en verde. El número no puede bajar.

---

## Gemini — barrido de colores pegados a mano

### El problema

`AGENTS.md` dice que no puede haber ni un color escrito a mano en el CSS: todo
sale de las variables de `styles.css`. La razón no es estética, es el modo
oscuro. Un `#ffffff` escrito a mano sigue siendo blanco cuando el resto de la
pantalla se vuelve oscura, y el texto desaparece.

Ya se barrieron 403 valores en rondas anteriores, pero **quedaron 60** repartidos
en 14 archivos. Verificalo vos mismo antes de empezar:

```bash
cd vitalis-frontend/src/app
grep -rhoE "#[0-9a-fA-F]{3,6}" . --include=*.css | wc -l
```

### Tus archivos

Estos y ningún otro:

| Archivo | Valores a reemplazar |
|---|---|
| `prescripciones/prescripciones.css` | 18 |
| `email-logs/email-logs.css` | 10 |
| `profesionales/profesionales.css` | 6 |
| `pacientes/pacientes.css` | 4 |
| `facturacion/facturacion.css` | 3 |
| `historia-clinica/historia-clinica.css` | 3 |
| `especialidades/especialidades.css` | 2 |
| `obras-sociales/obras-sociales.css` | 2 |
| `perfil/perfil.css` | 2 |
| `prestaciones/prestaciones.css` | 2 |
| `turnos/turnos.css` | 2 |
| `bloqueos/bloqueos.css` | 1 |
| `liquidaciones/liquidaciones.css` | 1 |
| `medicamentos/medicamentos.css` | 1 |

### Cómo reemplazar

Estas son las variables disponibles. Están todas definidas en `styles.css`, que
**no tenés que abrir ni modificar**:

```
Superficies    var(--bg-primary)  var(--bg-secondary)  var(--bg-card)
Texto          var(--text-primary)  var(--text-secondary)  var(--text-muted)
               var(--text-on-color)   <- texto blanco sobre fondos saturados
Marca          var(--color-primary)  var(--color-primary-hover)
               var(--color-primary-bg)  var(--color-primary-light)
Semánticos     var(--color-success)  var(--color-warning)  var(--color-danger)
               var(--color-info)  var(--color-accent)
Bordes         var(--border-color)  var(--border-light)
Visor de JSON  var(--code-bg)  var(--code-text)  var(--code-key)
               var(--code-value)  var(--code-highlight)  var(--code-highlight-bg)
```

**Criterio para elegir:** mirá qué representa el color, no qué tono es. Un gris
claro de fondo de tarjeta es `--bg-card` aunque el hex diga `#fefefe`. Un rojo de
error es `--color-danger` aunque sea un rojo distinto del token. La idea es que
el color exprese un rol, no un valor.

**Si un color no encaja en ninguna variable, no lo inventes ni agregues una
nueva** — `styles.css` es archivo compartido. Anotalo en tu informe con el
archivo, la línea, el valor y para qué se usa, y seguí con el resto.

### Cuándo terminaste

```bash
cd vitalis-frontend/src/app
grep -rhoE "#[0-9a-fA-F]{3,6}" . --include=*.css | wc -l    # tiene que dar 0

cd ../..
npx ng build
npx ng test --no-watch    # 37, no menos
```

Y después, con la aplicación corriendo, **cambiá el tema de Windows a oscuro** y
recorré las 14 pantallas que tocaste. No puede quedar texto ilegible, ni una
tarjeta blanca en medio de una pantalla oscura. Esto último no lo detecta ningún
comando: hay que mirarlo.

### No hagas

- No toques `styles.css`, `dashboard.css`, `auditorias.css` ni `app.config.ts`.
- No cambies tamaños, espaciados ni tipografías. **Solo colores.** Si de paso ves
  algo feo, anotalo en el informe y no lo toques.

---

## OpenCode / DeepSeek — dos tareas

### Tarea 1 (prioritaria): auditoría de seguridad, sin modificar nada

Esta semana se encontró un agujero probando el sistema a mano: un médico podía
registrar una consulta sobre el turno de otro profesional, porque el servicio
tomaba el `ProfesionalId` del cuerpo del pedido en vez del token. Se corrigió en
`ConsultaMedicaService`, `PrescripcionService` y `TurnoService`.

**La pregunta que nadie contestó todavía es si el mismo patrón está en otros
lados.** Tu tarea es contestarla.

Revisá estos servicios y sus controladores:

```
backend/src/Vitalis.Infrastructure/Services/FacturaService.cs
backend/src/Vitalis.Infrastructure/Services/LiquidacionService.cs
backend/src/Vitalis.Infrastructure/Services/PacienteService.cs
backend/src/Vitalis.Infrastructure/Services/SearchService.cs
backend/src/Vitalis.Infrastructure/Services/ReporteService.cs
backend/src/Vitalis.Infrastructure/Services/ReporteFacturacionService.cs
backend/src/Vitalis.Infrastructure/Services/BloqueoAgendaService.cs
backend/src/Vitalis.Api/Controllers/*.cs
```

Buscá tres cosas concretas:

1. **Identidad que viene del cliente.** Un `dto.ProfesionalId`, `dto.UsuarioId` o
   similar que se guarde o se use para filtrar sin cruzarlo contra el token.
   Compará con cómo quedó `ConsultaMedicaService.CrearAsync`, que es el patrón
   correcto: el dato se toma de la entidad de la base, no del pedido.
2. **Filtrado que ocurre en el navegador y no en el servidor.** El caso conocido
   era `turnos.ts`, que pedía todos los turnos y escondía los ajenos con un
   `.filter()`. Buscá si algún otro componente hace lo mismo (`grep -rn
   "\.filter(" vitalis-frontend/src/app --include=*.ts`) y, si lo hace, si el
   endpoint del backend devuelve datos que ese rol no debería ver.
3. **Endpoints sin `[Authorize]`, o con roles más amplios de lo necesario.**
   ¿Puede un recepcionista pedir los reportes de facturación? ¿Puede un médico
   listar todos los usuarios del sistema?

**Entregá un informe en `docs/16-hallazgos-de-seguridad.md`. No corrijas nada.**
Un arreglo de autorización cambia el comportamiento del sistema para todos los
roles y hay que decidirlo, no improvisarlo. Para cada hallazgo escribí:

- Archivo y línea
- Qué permite hacer hoy, en una frase concreta ("un recepcionista puede ver la
  facturación de toda la clínica")
- Si es de verdad un problema o es correcto que sea así, y por qué

Ese último punto es el importante. **Un informe con tres hallazgos reales vale
más que uno con veinte sospechas.** Si algo parece raro pero al seguir el código
resulta correcto, escribilo igual y decí que lo descartaste: eso evita que
alguien lo vuelva a investigar desde cero.

### Tarea 2: pruebas de los servicios del frontend

Hay 23 servicios en `vitalis-frontend/src/app/services/` y solo 4 tienen
pruebas. Escribí pruebas para estos cinco, **creando archivos nuevos**:

```
consulta-medica.service.spec.ts
prescripcion.service.spec.ts
email.service.spec.ts
usuario.service.spec.ts
factura.service.spec.ts
```

Copiá la forma de `turno.service.spec.ts`, que ya usa `HttpTestingController`.
Para cada servicio verificá: que llame a la URL correcta, con el método correcto,
que mande el cuerpo esperado, y que devuelva lo que responde el servidor.

**Solo archivos nuevos.** No modifiques los cuatro `.spec.ts` que ya existen ni
ningún `.service.ts`: si al escribir la prueba encontrás un bug en el servicio,
anotalo en tu informe en vez de arreglarlo.

### Cuándo terminaste

```bash
cd vitalis-frontend
npx ng test --no-watch    # 37 + las que agregaste

cd ../backend
dotnet test tests/Vitalis.Tests    # 112, no menos
```

---

## Claude — lo que queda de mi lado

- Verificar Docker de punta a punta con Tito
- Terminar el recorrido de prueba, bloques 3 a 5
- Documento de la tesina: incorporar los hallazgos de esta semana a la sección
  6.3 y actualizar el índice
- Decidir qué se hace con los hallazgos del informe de DeepSeek
- Archivos compartidos: `styles.css`, `app.config.ts`, `dashboard.*`,
  `Program.cs`, `DependencyInjection.cs`, `VitalisDbContext.cs`

---

## Si te chocás con otro

Pasó una vez que dos asistentes construyeron el mismo módulo en paralelo y casi
se sobrescribe trabajo terminado. Antes de escribir:

```bash
git log --oneline -10
git status
```

Si el archivo que ibas a tocar ya lo modificó otro, **pará y avisá**. No es una
carrera.
