# Plan de demostración para el día de la defensa

> Para no depender de que todo salga bien. Cada opción tiene su plan B, y todas
> se prueban **antes**, no ese día.
>
> El problema a resolver: la notebook donde vive el proyecto tiene salida VGA y
> puede que el proyector solo acepte HDMI.

---

## Antes que nada: dos cosas que hay que averiguar

**1. ¿Qué entradas tiene el proyector del aula?**

Una foto de la parte de atrás alcanza. La mayoría de los proyectores de
instituto en Argentina todavía tienen VGA — bastantes tienen VGA y *no* HDMI.
Si acepta VGA, no hay problema que resolver y el resto de este documento sobra.

**2. ¿La red del instituto deja que dos máquinas se vean entre sí?**

Muchas redes institucionales tienen *aislamiento de clientes* activado: cada
equipo llega a internet pero no ve a los demás. Eso rompe la Opción A. Se prueba
en dos minutos (más abajo) y tiene solución simple.

---

## Opción A — Tu notebook sirve, otra máquina proyecta

**La recomendada.** No requiere instalar nada en la otra máquina: solo un
navegador.

### Cómo funciona

Tu notebook corre Docker. La otra notebook abre el sistema por la red y proyecta
desde su propia salida HDMI. Tu máquina puede quedar cerrada a un costado.

Funciona por dos decisiones que ya están tomadas en el proyecto:

- `docker-compose.yml` publica el puerto 8080 en **todas** las interfaces de red,
  no solo en `localhost`.
- El frontend pide la API a `/api` de forma **relativa**, así que funciona desde
  cualquier dirección sin reconfigurar nada.

### Pasos

En tu notebook:

```powershell
cd "C:\Users\Tito\Desktop\New project"
docker compose up
ipconfig
```

De la salida de `ipconfig`, anotá **Dirección IPv4** (algo como `192.168.0.15`).

En la otra máquina, abrir en el navegador:

```
http://192.168.0.15:8080
```

### Probalo antes, con el celular

No hace falta otra notebook para verificar que funciona. Con Docker corriendo,
desde el celular conectado al mismo wifi abrí `http://TU-IP:8080`. Si carga ahí,
carga en cualquier equipo de la red.

### Si no carga: el plan B de esta opción

**Compartí internet desde tu celular** y conectá las dos notebooks a esa red.
La red pasa a ser tuya, sin aislamiento de clientes ni reglas ajenas. Volvé a
correr `ipconfig` porque la IP cambia al cambiar de red.

Esta variante es la más robusta de todo el documento: no depende de la
infraestructura del instituto.

### Si Windows pregunta por el firewall

Al levantar Docker por primera vez Windows puede pedir permiso de red. Hay que
permitirlo para **redes privadas**. Si ya se rechazó una vez:
Configuración → Red → Firewall → permitir Docker Desktop.

---

## Opción B — Conversor VGA a HDMI

Si preferís proyectar desde tu propia máquina.

Hace falta un **conversor activo** VGA → HDMI. La palabra "activo" importa: VGA
es analógico y HDMI digital, así que un cable pasivo **no funciona** por más que
los conectores entren. Tiene que ser un aparatito con chip, y suele llevar
alimentación por USB.

Se consigue en cualquier casa de computación y sale poco.

**Probalo con un televisor antes del día de la defensa.** Es hardware barato y
la calidad varía; conviene descubrir que no anda en tu casa y no en el aula.

---

## Opción C — Docker en la máquina de un compañero

Funciona, pero es la que más cosas necesita: Docker Desktop instalado, permisos
de administrador y a veces reiniciar. Por eso va tercera.

El truco es no depender de internet ni de esperar la compilación. Con todo
funcionando en tu máquina:

```powershell
cd "C:\Users\Tito\Desktop\New project"
docker save newproject-api newproject-frontend postgres:16-alpine -o vitalis-imagenes.tar
```

> Si `docker save` dice que no encuentra las imágenes, mirá cómo se llaman con
> `docker images` y usá esos nombres. Docker las nombra según la carpeta del
> proyecto.

Llevás en un pendrive el archivo `.tar` (cerca de 1 GB) y el `docker-compose.yml`.
En la otra máquina, en la misma carpeta:

```powershell
docker load -i vitalis-imagenes.tar
docker compose up
```

**Sin `--build`.** No compila ni descarga nada: arranca en segundos.

---

## Checklist de la semana previa

- [ ] Averiguar qué entradas tiene el proyector del aula
- [ ] Probar la Opción A desde el celular, en tu casa
- [ ] Probar la Opción A con el celular como punto de acceso
- [ ] Conseguir y probar el conversor, si el proyector no tiene VGA
- [ ] Dejar el `.tar` de las imágenes en un pendrive (respaldo de la Opción C)
- [ ] Ensayar la demo completa con `docs/09-guion-demo-y-defensa.md`

## Checklist del día

- [ ] Cargador de la notebook. Docker consume batería rápido.
- [ ] Levantar el sistema **antes** de entrar al aula y dejarlo corriendo
- [ ] Iniciar sesión antes de empezar, para no tipear contraseñas frente al jurado
- [ ] Celular cargado, por si hace falta compartir internet
- [ ] Pendrive con el `.tar` y el `docker-compose.yml`

---

## Si todo falla

Ningún jurado reprueba una tesis por un proyector. Si el día se pone en contra,
la defensa se da igual: el trabajo está en el documento, en el repositorio y en
la explicación que puedas dar. Tener 116 pruebas automáticas, un sistema que
levanta con un comando y un registro de auditoría de accesos a la historia
clínica se sostiene aunque nadie vea la pantalla.

Conviene llevar **capturas de las pantallas principales** en el celular o
impresas, como último respaldo:

- Panel principal
- Agenda semanal
- Historia clínica de un paciente
- Una receta lista para imprimir
- La pantalla de auditoría con un acceso registrado

---

## Datos de acceso

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | `admin@vitalis.local` | `Admin123!` |
| Médica (Laura Martínez, Pediatría) | `lmartinez@vitalis.local` | `Medico123!` |
| Recepcionista | `recepcion@vitalis.local` | `Recepcion123!` |

**Direcciones**

| Cómo | Dirección |
|---|---|
| Con Docker | `http://localhost:8080` |
| Desde otra máquina de la red | `http://TU-IP:8080` |
| Sin Docker (`dotnet run` + `ng serve`) | `http://localhost:4200` |
| Documentación de la API | `http://localhost:8080/api` → Swagger en el puerto 5004 sin Docker |
