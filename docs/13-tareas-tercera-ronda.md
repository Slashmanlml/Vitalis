# Tercera ronda de tareas

> Coordinación: Claude. Leé primero `AGENTS.md` en la raíz del proyecto: ahí están
> las reglas de territorio y de estilo que valen para todos.
>
> Estado verificado al momento de escribir esto: backend **100/100**, frontend
> **33 pruebas en verde**, compilación estricta de plantillas sin errores.

---

## Gemini — Rediseño de la Sala de Espera

### Por qué

Es **la pantalla que más se va a mirar en la defensa**. Es la única con
movimiento: el paciente llega, espera, lo llaman, entra. Si hay una pantalla que
tiene que verse impecable, es esta.

Hoy funciona bien, pero su estética quedó atrás: nació con una paleta violeta
propia que se migró a los tokens del sistema en una pasada mecánica, sin
rediseñarla. Se nota que es un injerto.

### Alcance

**Territorio:** `vitalis-frontend/src/app/sala-espera/` únicamente. No toques el
backend, ni las rutas, ni el menú.

**Qué debe lograr el rediseño:**

1. **Lectura a distancia.** Esta pantalla se proyecta o se deja en un monitor.
   Los nombres de pacientes y los tiempos de espera tienen que leerse de lejos:
   jerarquía tipográfica fuerte, no todo del mismo tamaño.

2. **El tiempo de espera como dato protagonista.** Hoy es un dato más. Debería
   ser lo que salta a la vista, porque es lo que le importa a quien mira la
   pantalla. Considerá destacar en color a quien lleva esperando demasiado —
   pero **con etiqueta de texto además del color**, nunca color solo.

3. **Los seis estados** (Solicitado, Confirmado, En espera, En atención,
   Atendido, Cancelado) tienen que distinguirse de un vistazo y mantener
   coherencia con el resto del sistema.

4. **El llamado a paciente** ya tiene una superposición a pantalla completa que
   funciona bien y es un buen momento visual. **Conservala**, pero revisá que su
   contraste y su tipografía estén a la altura del resto.

5. **Vacío digno.** Cuando no hay nadie esperando, la pantalla no puede verse
   rota. Un estado vacío bien resuelto dice mucho.

### Reglas

- **Cero colores hardcodeados.** Todo de los tokens de `styles.css`.
  Verificación: `grep -E "#[0-9a-fA-F]{3,6}" sala-espera.css` debe dar cero,
  salvo `#fff` sobre fondos de color sólido.
- Tiene que verse bien en **tema claro y oscuro**. No lo pruebes en uno solo.
- Nada de librerías nuevas.
- Si necesitás un dato que el backend no expone, **no inventes el endpoint**:
  anotalo y avisá.

### Terminado cuando

- [ ] `npx ng build` sin errores y `npx ng test --no-watch` en verde.
- [ ] Cero hexadecimales fuera de la excepción indicada.
- [ ] Se ve correcta en claro y en oscuro.
- [ ] Con la lista vacía, la pantalla sigue viéndose intencional.

---

## DeepSeek — Datos de demostración clínicos

### Por qué

`DbSeeder.cs` ya siembra roles, usuarios, obras sociales, especialidades,
profesionales, pacientes, turnos, prestaciones, facturas con pagos y
liquidaciones. Está bien.

Pero **no siembra nada clínico**. Eso significa que, en la defensa, cuando se
abra Historia Clínica o Prescripciones, las pantallas van a aparecer **vacías**.
Son dos de los módulos más importantes del sistema y se verían como si no
estuvieran hechos.

Tu tarea es cerrar ese hueco.

### Alcance

**Territorio:** únicamente
`backend/src/Vitalis.Infrastructure/Persistence/DbSeeder.cs`.
No toques ningún otro archivo.

### Qué sembrar

Seguí **exactamente el patrón que ya usa el archivo**: cada bloque empieza
comprobando si la tabla está vacía (`if (!await context.X.AnyAsync(...))`) y sólo
entonces inserta. Es lo que evita duplicar datos en cada arranque. No lo cambies.

Agregá, en este orden (importa, porque unos dependen de otros):

1. **Medicamentos** (~12): nombres reales y comunes con su presentación —
   Ibuprofeno 400 mg, Amoxicilina 500 mg, Enalapril 10 mg, Metformina 850 mg,
   Losartán 50 mg, Omeprazol 20 mg, Paracetamol 500 mg, Salbutamol aerosol,
   Levotiroxina 100 mcg, Atorvastatina 20 mg, Diclofenac 75 mg, Amlodipina 5 mg.

2. **Consultas médicas** (~15): una por cada turno que ya esté en estado
   "Atendido". **No inventes turnos nuevos**: recorré los que el seeder ya creó y
   filtrá por ese estado. Cada consulta necesita motivo, diagnóstico, evolución e
   indicaciones, coherentes entre sí y con la especialidad del profesional del
   turno. Un cardiólogo no diagnostica otitis.

3. **Antecedentes** (~10) repartidos entre varios pacientes: tipo (Quirúrgico,
   Familiar, Patológico, Alérgico) y descripción.

4. **Alergias** (~6): sustancia, reacción y severidad (Leve, Moderada, Grave).

5. **Prescripciones** (~8) asociadas a consultas ya creadas, cada una con uno a
   tres medicamentos del catálogo, con dosis, frecuencia y duración plausibles y
   **coherentes con el diagnóstico de esa consulta**.

6. **Bloqueos de agenda** (2): uno vigente y uno pasado, con motivos reales
   ("Congreso de cardiología", "Licencia médica").

### Reglas

- **Nombres y datos verosímiles, en español rioplatense.** Nada de "Paciente 1"
  ni "test". Esto se proyecta ante un jurado.
- **Coherencia clínica.** El diagnóstico, la medicación y la especialidad tienen
  que cerrar entre sí. Es el detalle que separa una demo creíble de una que
  parece de relleno.
- **Fechas relativas a hoy**, nunca fijas: usá `DateTime.UtcNow.AddDays(-n)` para
  que la demo siga teniendo sentido dentro de un mes.
- Todas las fechas van en **UTC** (`DateTime.SpecifyKind(..., DateTimeKind.Utc)`).
  PostgreSQL rechaza fechas sin zona horaria: ya hubo un defecto por esto y está
  documentado en la tesina.
- **No inventes datos personales de gente real.**

### Terminado cuando

- [ ] `dotnet build` sin errores.
- [ ] `dotnet test tests\Vitalis.Tests` sigue en **100** o más. Los tests usan su
      propia siembra en memoria, así que esto no debería tocarlos: si algo se
      rompe, revisá qué tocaste de más.
- [ ] Con la base borrada y la API levantada de cero, Historia Clínica y
      Prescripciones muestran datos.

---

## Recordatorio para los dos

Si encontrás algo mal en código que **no es tuyo**, no lo arregles: anotalo y
avisá. Con tres asistentes trabajando en paralelo, una corrección por izquierda
es la forma más rápida de pisarle el trabajo a otro.
