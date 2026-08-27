# Material para incorporar a la tesina

> Lo hecho en las últimas rondas, organizado por capítulo, con los datos duros
> listos para citar. No es texto para copiar tal cual: son los hechos y los
> argumentos, para que los escribas con tu voz.

---

## 1. El hilo conductor

Si hay una sola idea que ordene este capítulo, es esta:

> **Las pruebas automáticas verifican lo que se pensó verificar. Ejecutar el
> sistema revela lo que no se pensó.**

Vitalis tenía 100 pruebas en verde y, al levantarlo contra una base PostgreSQL
real por primera vez, **no arrancaba**. Eso no es un fracaso de las pruebas: es
una demostración de qué cubre cada técnica de verificación y qué no. Sirve para
la sección de limitaciones y para las conclusiones.

---

## 2. Defectos que solo aparecieron al ejecutar

Los tres se detectaron corriendo el sistema, no compilándolo ni probándolo.

### 2.1 Fechas sin zona horaria (impedía el arranque)

El sembrador creaba fechas con `DateTimeKind.Unspecified`. Npgsql exige
`Utc` para columnas `timestamp with time zone` y lanza una excepción.

**Por qué las pruebas no lo vieron:** la batería usa el proveedor *InMemory* de
EF Core, que no aplica las reglas de tipo de PostgreSQL. En memoria la fecha se
guarda sin objetar nada.

**Consecuencia:** el sistema no arrancaba en ninguna máquina con base nueva.

### 2.2 Carpeta de archivos subidos inexistente

`UseStaticFiles()` se asocia a la carpeta raíz web **al arrancar**. Si la carpeta
no existe en ese momento, no se sirve nada aunque se cree después. Las fotos se
guardaban pero no se veían.

### 2.3 La especialidad del profesional nunca se resolvía

El sembrador elegía las plantillas clínicas según la especialidad, pero la
propiedad de navegación no estaba cargada. Resultado: **los cinco profesionales
diagnosticaban las mismas tres afecciones de Clínica Médica**, y se generaban 2
recetas en lugar de 8.

**Vale la pena contar el proceso, no solo el resultado.** El primer diagnóstico
—agregar un `Include`— era razonable y **no funcionó**. Se comprobó consultando
la base directamente. La solución definitiva fue eliminar la dependencia de la
navegación y usar un diccionario explícito de identificador a nombre.

> Es un buen ejemplo para la defensa: una hipótesis plausible que se descarta con
> evidencia. Muestra método, no suerte.

---

## 3. Seguridad y control de acceso

Es el hallazgo más fuerte del trabajo y el que más peso tiene ante un jurado.

### 3.1 El defecto

Un profesional podía **registrar consultas médicas sobre turnos de otro
profesional**. El servicio validaba que el turno, el paciente y el profesional
*existieran*, pero nunca que el médico autenticado fuera el del turno. Además
guardaba el identificador del profesional **que enviaba el navegador**.

### 3.2 El defecto asociado

El filtrado por rol se hacía **en el navegador**: el servidor devolvía la agenda
completa de la clínica y el frontend ocultaba lo ajeno con un `filter`. Los datos
viajaban igual y eran visibles desde las herramientas de desarrollo. En un
sistema de salud eso constituye exposición de datos sensibles.

### 3.3 La corrección, y el principio detrás

No se agregó una validación: **se eliminó la posibilidad**.

```
Antes                          Después
PacienteId = dto.PacienteId    PacienteId = turno.PacienteId
```

El turno es una fila real de la base, con claves foráneas garantizadas por el
motor. El campo equivalente del pedido **ya no se lee**.

> **Principio: validar un dato que controla el cliente es más débil que no
> usarlo.** Una validación puede tener un caso no contemplado; un dato que no se
> lee no tiene casos.

La identidad del profesional se resuelve desde el token firmado, a través de un
servicio `IUsuarioActual` que usa el vínculo `Profesional.UsuarioId` —presente en
el modelo desde el inicio y sin utilizar—. Se agregó `ForbiddenException`,
mapeada a HTTP 403. El filtrado por profesional pasó al servidor: **los datos de
otros profesionales ya no salen de la base**.

### 3.4 Auditoría de accesos de lectura

Pregunta que surge naturalmente: *¿puede un médico leer la historia clínica de
cualquier paciente?*

**Sí, y debe poder.** La historia clínica pertenece al paciente, no al médico.
Negar el acceso a antecedentes o alergias sería más peligroso que concederlo,
especialmente en una urgencia.

La contracara de permitir es **registrar**. El mecanismo de auditoría existente
se apoyaba en `SaveChanges`, de modo que solo veía escrituras: se podía consultar
una historia clínica sin dejar rastro. Se incorporó el registro de accesos de
lectura con acción `CONSULTAR`.

Decisión de diseño: **falla cerrado**. Si el acceso no puede registrarse, el
acceso no ocurre. No compromete la disponibilidad, porque la auditoría escribe en
la misma base de la que se acaba de leer.

> Distinción para la defensa: **leer** la historia de un paciente es continuidad
> de la atención y se controla auditando; **escribir** sobre el turno de otro es
> un problema de autoría y se controla bloqueando.

---

## 4. Integridad de datos

En el módulo de facturación, el importe de un pago no tenía validación de rango.
Un importe negativo se aceptaba y se sumaba, con lo que **una factura saldada
podía volver al estado "Pago Parcial"**. Una factura ya pagada tampoco rechazaba
pagos nuevos.

También se detectó que los errores de facturación usaban excepciones genéricas
que el middleware no traduce: consultar una factura inexistente devolvía **500
error interno** en lugar de **404 no encontrada**, ocultando el motivo real.

> Observación metodológica: el hallazgo del importe negativo apareció **después**
> de una auditoría de seguridad que no lo detectó, porque esa auditoría buscaba
> un patrón distinto (identidad tomada del cliente). **Un revisor encuentra lo
> que fue a buscar.** De ahí el valor de revisiones con criterios diferentes.

---

## 5. Interfaz, accesibilidad y rendimiento

### 5.1 Sistema de diseño por tokens

Se eliminaron 463 colores escritos a mano, reemplazados por variables CSS. No es
una cuestión estética: el modo oscuro **funcionaba en 4 de 19 pantallas**, porque
un valor fijo no cambia cuando cambia el tema.

Se desacopló el matiz de marca del matiz de las superficies, de modo que cambiar
la identidad visual ya no tiñe fondos ni textos.

### 5.2 Accesibilidad

Una paleta de cuatro colores para estados se descartó tras validarla: verde y
ámbar quedaban indistinguibles bajo daltonismo rojo-verde (diferencia perceptual
5,1 sobre un mínimo de 8). Se rediseñó con insignias etiquetadas.

> **Regla adoptada: ningún estado se comunica solo por color.**

### 5.3 Un caso de estudio sobre automatización

El barrido de colores se delegó a un asistente automático. El verificador
—contar valores hexadecimales— dio **cero**, es decir "correcto". Y sin embargo
el resultado introdujo un defecto: los valores fijos de la vista de receta
**estaban puestos a propósito**, para que se imprimiera legible
independientemente del tema. Al convertirlos en variables, imprimir con tema
oscuro producía una hoja negra.

Se resolvió con tokens de *papel*, sin variante oscura, documentados con su
justificación.

> **Conclusiones: una métrica en cero indica que la métrica llegó a cero, no que
> el trabajo sea correcto. Y toda regla debe documentar sus excepciones, o serán
> "corregidas".**

### 5.4 Carga diferida de rutas

Las 20 pantallas se cargaban antes de mostrar el formulario de acceso: 880 kB
iniciales, por encima del presupuesto de 600 kB. Se convirtieron a carga
diferida: cada pantalla viaja en su propio archivo y se descarga al usarse.

Justificación funcional: una recepcionista utiliza tres de las diecinueve
pantallas; no corresponde que su navegador descargue el módulo de liquidaciones.

---

## 6. Despliegue reproducible

Se incorporó contenedorización con Docker: tres contenedores (PostgreSQL, API,
frontend) que se levantan con **un solo comando** en cualquier equipo, sin
instalar .NET, Node ni PostgreSQL.

Puntos técnicos que conviene mencionar:

- **Construcción en dos etapas.** La imagen final parte del entorno de ejecución
  y recibe solo el resultado compilado: no contiene compilador ni código fuente.
  Menos peso y menor superficie de ataque.
- **Verificación de estado.** Sin ella, la API arranca antes de que la base
  acepte conexiones y falla al migrar.
- **Origen unificado.** nginx sirve el frontend y actúa de puente hacia la API,
  de modo que ambos comparten origen y **CORS deja de intervenir**. No se
  desactivó ni se configuró de forma permisiva: dejó de aplicar.

> Efecto colateral no buscado: al pedir la API con una ruta relativa, el sistema
> queda accesible desde **cualquier equipo de la red local** sin reconfigurar
> nada. Una decisión correcta resolvió un segundo problema que no se había
> planteado.

---

## 7. Verificación: los números

| Momento | Backend | Frontend |
|---|---|---|
| Inicio del período | 65 | 5 |
| Tras módulos de bloqueo y reportes | 79 | 33 |
| Tras control de acceso por profesional | 109 | 37 |
| Estado actual | **116** | **37** |

Compilación sin advertencias en ambos proyectos.

**Criterio adoptado:** cada defecto encontrado quedó cubierto por una prueba que
falla si el defecto reaparece. Eso es prueba de regresión, y es lo que permite
afirmar que una corrección no volverá atrás.

### Un matiz metodológico que conviene explicar

Al corregir el control de acceso, cuatro pruebas existentes fallaron. **No se
eliminaron: se reescribieron.** Verificaban que un identificador inexistente en
el pedido produjera un error; ese comportamiento dejó de ser alcanzable porque el
servicio ya no lee ese campo. La intención original —que no se creen registros
huérfanos— sigue vigente y ahora se garantiza por construcción.

> Distinción para la defensa: una prueba en rojo puede significar **"rompiste
> algo"** o **"cambió el contrato"**. Se ven igual desde afuera. Lo único que las
> separa es preguntarse qué protegía esa prueba y si sigue protegido. Ante la
> duda, se corrige el código, no la prueba.

---

## 8. Dónde ubicar cada cosa

| Contenido | Sección sugerida |
|---|---|
| Defectos detectados al ejecutar (2) | Pruebas y validación / Limitaciones |
| Control de acceso y auditoría (3) | Seguridad; también Resultados |
| Integridad de facturación (4) | Pruebas y validación |
| Tokens y accesibilidad (5.1–5.2) | Diseño de interfaz |
| Caso de automatización (5.3) | Conclusiones o Metodología |
| Carga diferida (5.4) | Arquitectura del frontend |
| Docker (6) | Implementación y despliegue |
| Números y criterio de regresión (7) | Pruebas y validación |

---

## 9. Tres frases para la defensa oral

Si te quedás en blanco, estas tres resumen el trabajo:

1. *"Las pruebas verifican lo que uno pensó verificar; ejecutar el sistema revela
   lo que no. Con cien pruebas en verde, el sistema no arrancaba contra una base
   real."*

2. *"Cuando un dato lo controla el cliente, validarlo es más débil que no usarlo.
   La consulta médica toma el paciente y el profesional del turno, no del
   pedido."*

3. *"Leer la historia clínica de un paciente está permitido, porque la atención
   lo requiere. La contracara de permitir es registrar: cada acceso queda
   auditado."*
