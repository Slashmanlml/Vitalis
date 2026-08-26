# Lista de prueba del sistema — antes de la defensa

> Para recorrer Vitalis de punta a punta y encontrar los problemas ahora y no
> frente a la mesa. Está pensada para hacerse de corrido, en orden.
>
> **Si algo falla, no sigas de largo:** copiá el error completo y avisá. Cada
> bloque depende de que el anterior haya salido bien.

---

## Antes de empezar: dos cosas que conviene saber

**Vas a necesitar dos terminales abiertas al mismo tiempo.** La API y el frontend
son dos programas distintos y los dos quedan corriendo. Si cerrás la terminal, se
apagan.

- Terminal 1 → la API (backend), en el puerto **5004**
- Terminal 2 → el frontend, en el puerto **4200**

**Los datos de acceso** que siembra el sistema solo:

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | `admin@vitalis.local` | `Admin123!` |
| Médico (Laura Martínez, Pediatría) | `lmartinez@vitalis.local` | `Medico123!` |
| Recepcionista | `recepcion@vitalis.local` | `Recepcion123!` |

---

## Bloque 1 — Base de datos desde cero

Esto es lo más importante de toda la lista. El sistema siembra sus datos al
arrancar, y ese código nunca se ejecutó contra una base real.

### 1.1 Borrar la base

En PowerShell:

```powershell
cd "C:\Users\Tito\Desktop\New project\backend"
psql -U postgres -c "DROP DATABASE IF EXISTS vitalis;"
psql -U postgres -c "CREATE DATABASE vitalis;"
```

Te va a pedir la contraseña de PostgreSQL una vez por comando.

> Si `psql` no se reconoce como comando, PostgreSQL no está en el PATH. Avisame y
> te paso la alternativa desde pgAdmin.

**Qué deberías ver:** `DROP DATABASE` y `CREATE DATABASE`, sin errores.

### 1.2 Levantar la API

```powershell
cd "C:\Users\Tito\Desktop\New project\backend\src\Vitalis.Api"
dotnet run
```

**Esta terminal queda ocupada.** No la cierres.

**Qué deberías ver:** varias líneas de log, entre ellas mensajes de siembra
(`Roles iniciales creados`, `Prescripciones de ejemplo sembradas`, etc.), y al
final:

```
Now listening on: http://localhost:5004
Application started.
```

**Si en cambio ves una excepción**, copiá el bloque completo del error. Ahí está
el problema que veníamos a buscar.

### 1.3 Confirmar que la API responde

Abrí en el navegador: **http://localhost:5004/swagger**

**Qué deberías ver:** la lista de endpoints agrupados por controlador.

- [ ] La base se creó vacía
- [ ] La API arrancó sin excepciones
- [ ] Swagger abre y muestra los controladores

---

## Bloque 2 — Frontend

### 2.1 Levantar el frontend

En una **segunda** terminal (dejá la primera con la API corriendo):

```powershell
cd "C:\Users\Tito\Desktop\New project\vitalis-frontend"
ng serve
```

**Qué deberías ver:** `Application bundle generation complete` y
`Local: http://localhost:4200/`.

### 2.2 Entrar

Abrí **http://localhost:4200** e ingresá como **administrador**.

**Qué deberías ver:** el panel principal con la actividad del día.

- [ ] El frontend compila y abre
- [ ] El login funciona
- [ ] El panel muestra números, no ceros ni pantalla en blanco

> Si el panel muestra todo en cero: puede ser correcto, si el seeder no generó
> turnos para hoy. Anotalo y seguimos: lo revisamos después.

---

## Bloque 3 — Recorrida de pantallas

Entrá a cada una desde el menú lateral y fijate únicamente **si carga y si
muestra datos**. Todavía no toques nada.

| # | Pantalla | ¿Carga? | ¿Tiene datos? |
|---|---|---|---|
| 1 | Pacientes | | |
| 2 | Profesionales | | |
| 3 | Turnos (vista Listado) | | |
| 4 | Turnos (vista Agenda) | | |
| 5 | Sala de Espera | | |
| 6 | Historia Clínica | | |
| 7 | Prescripciones | | |
| 8 | Obras Sociales | | |
| 9 | Especialidades | | |
| 10 | Medicamentos | | |
| 11 | Prestaciones | | |
| 12 | Facturación | | |
| 13 | Liquidaciones | | |
| 14 | Reportes | | |
| 15 | Bloqueo de Agenda | | |
| 16 | Auditorías | | |
| 17 | Notificaciones | | |
| 18 | Usuarios | | |
| 19 | Perfil | | |

**Las dos que más importan** son Historia Clínica y Prescripciones: son las que
estaban vacías antes de esta ronda. Para verlas, elegí un paciente en el
desplegable de arriba.

**Truco para detectar errores invisibles:** apretá **F12** y mirá la pestaña
*Console*. Si aparece algo en rojo, copialo aunque la pantalla se vea bien.

---

## Bloque 4 — El circuito completo

Acá se prueba que los módulos se hablen entre sí. Es el corazón de la demo.

### 4.1 Como recepcionista

Salí de la sesión y entrá con `recepcion@vitalis.local`.

1. **Turnos → Nuevo Turno.** Creá un turno para hoy, dentro del horario de
   atención (lunes a viernes, de 08:00 a 20:00).
   - [ ] Se creó y aparece en el listado
2. **Confirmalo** con el botón de tilde.
   - [ ] El estado cambia a Confirmado
3. **Sala de Espera.** Marcá que el paciente llegó.
   - [ ] Aparece en la lista de espera

### 4.2 Como médico

Salí y entrá con `lmartinez@vitalis.local`.

4. **Turnos.** Fijate que veas **solo los turnos de Laura Martínez**, no los de
   toda la clínica.
   - [ ] El filtro por rol funciona
5. **Historia Clínica.** Elegí un paciente y registrá una consulta sobre un turno
   existente.
   - [ ] La consulta se guarda y aparece en el historial
6. **Prescripciones.** Emití una receta sobre esa consulta.
   - [ ] La receta se guarda
   - [ ] El formulario abre **vacío** (sin dosis precargada)
   - [ ] Se puede imprimir

### 4.3 Como administrador

Volvé a entrar como `admin@vitalis.local`.

7. **Reportes.** Mirá los indicadores y probá una consulta de detalle por
   profesional con rango de fechas.
   - [ ] Los números tienen sentido
   - [ ] La exportación a CSV descarga un archivo
8. **Bloqueo de Agenda.** Creá un bloqueo para un profesional en un rango donde
   haya turnos, y **revisá el impacto antes de confirmar**.
   - [ ] Muestra cuántos turnos se van a cancelar
   - [ ] Al confirmar, esos turnos quedan en Cancelado
9. **Notificaciones.** Fijate que estén registrados los avisos generados por lo
   que hiciste recién.
   - [ ] Se distingue lo emitido por el sistema de lo simulado
10. **Auditorías.** Buscá los registros de las operaciones que hiciste.
    - [ ] Figura tu usuario real, no un valor fijo

---

## Bloque 5 — Detalles que el jurado suele mirar

- [ ] **Modo oscuro.** Cambiá el tema del sistema operativo y recorré tres o
      cuatro pantallas. No debería quedar texto ilegible en ningún lado.
- [ ] **Cerrar sesión** y volver a entrar.
- [ ] **Entrar a una URL prohibida a mano.** Como recepcionista, escribí
      `http://localhost:4200/dashboard/usuarios` en la barra de direcciones. No
      debería dejarte, o el backend debería responder 403.
- [ ] **Pantalla angosta.** Achicá la ventana del navegador a la mitad. Nada
      debería salirse ni superponerse.

---

## Qué hacer con lo que encuentres

Anotá cada problema con estas tres cosas, que es lo que hace falta para
arreglarlo:

1. **En qué pantalla y qué hiciste**
2. **Qué esperabas y qué pasó**
3. **El error de la consola (F12) o de la terminal de la API**, completo

No intentes arreglarlo vos en el momento. Juntá la lista y la resolvemos de una,
que así no se pisa con lo que estén haciendo los otros asistentes.
