# Despliegue en producción

Cómo pasa Vitalis de la PC a un sitio público con dominio propio, HTTPS y
correo profesional, y cómo se actualiza solo cada vez que se sube código a
`main`.

## 1. Cómo queda armado

```
 Visitante ──HTTPS──▶ Cloudflare ══ túnel (saliente) ══▶ cloudflared ──▶ nginx ──▶ API ──▶ PostgreSQL
                     (DNS, TLS,                         └── red "borde" ──┘└──── red "interna" ────┘
                      firewall)
```

- **Cloudflare** atiende el dominio: DNS, certificado HTTPS, protección contra
  ataques y firewall de aplicación. La IP del servidor no se publica.
- **Túnel**: el contenedor `cloudflared` abre una conexión *saliente* hacia
  Cloudflare y por ahí entran los pedidos. El servidor no abre el 80 ni el 443;
  el firewall rechaza todo lo entrante salvo SSH.
- **Dos redes de Docker**: `cloudflared` solo ve a nginx. La API y la base están
  en otra red; aunque alguien comprometiera el túnel, no llega a la base.
- **Imágenes**: las arma GitHub Actions y las publica en GitHub Packages
  (`ghcr.io`). El servidor solo las descarga; no compila nada.

| Archivo | Para qué |
| --- | --- |
| `docker-compose.prod.yml` | Los cinco contenedores del servidor (base, API, nginx, túnel, respaldo) |
| `.env.produccion.example` | Plantilla de las variables y claves del servidor |
| `vitalis-frontend/nginx.conf` | IP real del visitante, HTTPS detectado, HSTS, caché, límite de subida |
| `scripts/servidor/preparar-servidor.sh` | Blindaje inicial del servidor |
| `.github/workflows/ci.yml` | Pruebas, publicación de imágenes y despliegue |

## 2. Prueba gratis hoy, sin dominio

Cloudflare da túneles rápidos sin cuenta: una dirección provisoria
`https://algo-al-azar.trycloudflare.com` que apunta a tu PC.

```powershell
cd C:\Users\Tito\Proyectos\Vitalis\vitalis
docker compose --profile tunel up --build
```

En el log aparece un recuadro con la dirección. Abrila desde el celular con
datos móviles (no el wifi de casa) y entrá con `admin@vitalis.local`. Se corta
con `Ctrl+C`. Mientras esté abierta, cualquiera con el enlace ve el sistema con
los datos de prueba: no lo dejes prendido de más.

Limitación: los correos arman sus enlaces con `URL_FRONTEND` del `.env`
(`http://localhost:8080`), así que no sirven fuera de la PC. Para eso está el
dominio real.

## 3. El día del dominio

### 3.1 Comprar el dominio

Cloudflare → **Domain Registration → Register Domains** → buscar, por ejemplo,
`vitalis-salud.com` → comprar. Cloudflare vende al costo (unos 10 USD por año)
y el dominio queda ya configurado en su DNS.

Recomendado en el dominio, apenas comprado:

- **SSL/TLS → Edge Certificates**: *Always Use HTTPS* activado y
  *Minimum TLS Version* en 1.2.
- **Security → WAF**: dejar activas las reglas administradas gratuitas.

### 3.2 Correo con Zoho Mail

1. Alta en <https://www.zoho.com/mail/> con el dominio propio. Zoho pide
   verificar que el dominio es tuyo con un registro **TXT**: se agrega en
   Cloudflare → **DNS → Records**.
2. Zoho muestra los registros que faltan. Todos se cargan en Cloudflare con la
   nube **gris** (*DNS only*): el correo no pasa por el proxy.

   | Tipo | Nombre | Valor (lo da Zoho) | Para qué |
   | --- | --- | --- | --- |
   | MX | `@` | `mx.zoho.com` (10), `mx2.zoho.com` (20), `mx3.zoho.com` (50) | Recibir correo |
   | TXT | `@` | `v=spf1 include:zoho.com ~all` | SPF: quién puede mandar por el dominio |
   | TXT | `zmail._domainkey` | la clave larga que genera Zoho | DKIM: firma de cada correo |
   | TXT | `_dmarc` | `v=DMARC1; p=none; rua=mailto:tu-casilla@tu-dominio` | DMARC: qué hacer con correos que fallan |

   Empezar DMARC con `p=none` (solo informa). Cuando los informes muestren que
   todo sale firmado, pasarlo a `p=quarantine`.
3. Crear la casilla que manda los correos del sistema, por ejemplo
   `no-responder@vitalis-salud.com`, y una personal para vos.
4. Para que la API pueda mandar por SMTP: activar la verificación en dos pasos
   en Zoho y generar una **contraseña de aplicación** (*Mi cuenta → Seguridad →
   Contraseñas específicas de aplicación*). Va en `SMTP_PASSWORD`.
5. El servidor SMTP con dominio propio es `smtppro.zoho.com`, puerto 587. Zoho
   lo confirma en *Configuración → Cuentas de correo*.

Ojo con el plan: el plan gratuito de Zoho no incluye acceso por IMAP/POP y
puede no permitir SMTP desde aplicaciones. Si la contraseña de aplicación no
funciona, el plan *Mail Lite* (alrededor de 1 USD por usuario por mes) lo
habilita. Para probar mientras tanto, el sistema sigue funcionando con Gmail.

### 3.3 El servidor (VPS)

Cualquier proveedor con Ubuntu 24.04 y 2 GB de RAM alcanza (Vultr o
DigitalOcean, región São Paulo, unos 12 USD por mes).

1. **Clave SSH** en tu PC (una sola vez). Pedí una frase de paso y guardala en
   Bitwarden:

   ```powershell
   ssh-keygen -t ed25519 -C "tito-pc"
   Get-Content $env:USERPROFILE\.ssh\id_ed25519.pub
   ```

   Lo que imprime la segunda línea (la clave **pública**) es lo que se pega en
   el proveedor al crear el servidor. La privada (`id_ed25519`, sin `.pub`)
   nunca sale de tu PC.
2. Crear el servidor con esa clave. Anotar su IP.
3. Copiar y correr el script de preparación:

   ```powershell
   cd C:\Users\Tito\Proyectos\Vitalis\vitalis
   scp scripts\servidor\preparar-servidor.sh root@IP_DEL_SERVIDOR:/root/
   ssh root@IP_DEL_SERVIDOR "bash /root/preparar-servidor.sh"
   ```

   Pide una contraseña para el usuario `vitalis` (solo para `sudo`). Al final,
   **sin cerrar esa sesión**, probá en otra terminal `ssh vitalis@IP_DEL_SERVIDOR`.
   Desde ahí, root ya no puede entrar por SSH.

### 3.4 El túnel

Cloudflare → **Zero Trust → Networks → Tunnels → Create a tunnel** →
*Cloudflared* → nombre `vitalis`. En el paso de instalación, elegir *Docker* y
copiar **solo el token** (el texto largo después de `--token`). Va en
`TUNNEL_TOKEN`; no se pega en ningún otro lado.

En **Public hostnames** agregar:

| Subdominio | Dominio | Servicio |
| --- | --- | --- |
| *(vacío)* | `vitalis-salud.com` | `HTTP` → `frontend:80` |

Cloudflare crea solo el registro DNS del dominio.

### 3.5 Primer arranque

En el servidor, como `vitalis`:

```bash
cd /opt/vitalis
nano .env          # pegar el contenido de .env.produccion.example y completar
chmod 600 .env
```

Desde la PC, copiar el compose:

```powershell
cd C:\Users\Tito\Proyectos\Vitalis\vitalis
scp docker-compose.prod.yml vitalis@IP_DEL_SERVIDOR:/opt/vitalis/
```

Y en el servidor:

```bash
cd /opt/vitalis
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml logs -f api
```

Cuando el log diga que la API escucha, abrir `https://vitalis-salud.com` y
entrar con `ADMIN_EMAIL` / `ADMIN_PASSWORD`. Cambiar esa contraseña desde el
sistema y borrar `ADMIN_PASSWORD` del `.env`.

Las imágenes se publican la primera vez que algo entra a `main` (ver 3.6). Si
`pull` dice *denied*, en GitHub → tu perfil → **Packages** → cada imagen →
*Package settings* → cambiar la visibilidad a pública (no tienen secretos: las
claves viven en el `.env` del servidor).

### 3.6 Despliegue automático

Desde ese momento, cada push a `main` corre las pruebas, publica las imágenes
y actualiza el servidor solo. Para encenderlo:

1. Una clave SSH **solo para GitHub** (en la PC). Cuando pida frase de paso,
   apretar Enter dos veces: la usa una máquina, no una persona.

   ```powershell
   ssh-keygen -t ed25519 -C "github-despliegue" -f $env:USERPROFILE\.ssh\vitalis_despliegue
   ```

   Agregar la pública al servidor:

   ```powershell
   Get-Content $env:USERPROFILE\.ssh\vitalis_despliegue.pub | ssh vitalis@IP_DEL_SERVIDOR "cat >> ~/.ssh/authorized_keys"
   ```

2. La huella del servidor, tomada desde tu PC (que ya confía en él):

   ```powershell
   ssh-keyscan -t ed25519 IP_DEL_SERVIDOR
   ```

3. En GitHub → el repositorio → **Settings → Environments → New environment**
   → `produccion`. Ahí, en *Environment secrets*:

   | Secreto | Valor |
   | --- | --- |
   | `SSH_HOST` | la IP del servidor |
   | `SSH_USUARIO` | `vitalis` |
   | `SSH_CLAVE_PRIVADA` | el contenido de `vitalis_despliegue` (sin `.pub`) |
   | `SSH_KNOWN_HOSTS` | la línea que imprimió `ssh-keyscan` |

   Opcional y recomendable: *Required reviewers* con tu usuario. Así cada
   despliegue espera un clic tuyo.
4. **Settings → Secrets and variables → Actions → Variables**:
   `DESPLIEGUE_HABILITADO` = `true` y `URL_SITIO` = `https://vitalis-salud.com`.
5. Borrar de la PC la clave privada de despliegue una vez cargada en GitHub.

Para que algo llegue a `main`: abrir un *pull request* de la rama de trabajo
hacia `main` en GitHub, esperar el tilde verde y hacer *Merge*.

El trabajo **Desplegar** copia el compose, baja las imágenes nuevas, reinicia
lo que cambió y consulta `/api/health` a través de Cloudflare. Si no responde
en un minuto, queda en rojo.

## 4. Operación

**Volver a una versión anterior.** Cada imagen tiene una etiqueta fija
`sha-xxxxxxx` (los primeros 7 caracteres del commit). En el servidor:

```bash
cd /opt/vitalis
VERSION=sha-1234567 docker compose -f docker-compose.prod.yml up -d
```

Ojo: si la versión nueva traía una migración de base, volver atrás la API no
deshace la migración. Antes de un cambio de modelo, hacer un respaldo a mano.

**Respaldos.** El contenedor `respaldo` deja cada día en
`/opt/vitalis/respaldos` un volcado de la base (`vitalis_FECHA.dump`) y los
adjuntos (`adjuntos_FECHA.tar.gz`), y conserva `RESPALDOS_DIAS` días. Una copia
en el mismo servidor no sirve si se pierde el servidor: bajarlos a la PC de vez
en cuando:

```powershell
cd C:\Users\Tito\Proyectos\Vitalis
scp -r vitalis@IP_DEL_SERVIDOR:/opt/vitalis/respaldos .\respaldos-servidor
```

**Restaurar** un volcado (borra lo que haya en la base, hacerlo con cuidado):

```bash
cd /opt/vitalis
docker compose -f docker-compose.prod.yml stop api
docker compose -f docker-compose.prod.yml exec -T db pg_restore -U postgres -d vitalis --clean --if-exists < respaldos/vitalis_FECHA.dump
docker compose -f docker-compose.prod.yml start api
```

Un respaldo que nunca se probó restaurar no es un respaldo: conviene ensayarlo
una vez en la PC.

**Ver qué pasa:**

```bash
cd /opt/vitalis
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs --tail 100 api
```

## 5. Para la defensa: qué controles agrega el despliegue

| Riesgo | Control |
| --- | --- |
| Servidor expuesto a escaneos y ataques directos | Sin puertos web abiertos: todo entra por el túnel. Firewall con solo SSH. IP no publicada |
| Robo de contraseña de SSH | SSH solo con clave, sin root, 3 intentos, fail2ban |
| Vulnerabilidades del sistema operativo | Actualizaciones de seguridad automáticas |
| Tráfico interceptado | HTTPS obligatorio en Cloudflare, TLS 1.2 mínimo, HSTS |
| Un contenedor comprometido alcanza la base | Dos redes separadas; cloudflared no ve la API ni la base. Contenedores sin privilegios nuevos y API sin capacidades |
| Límite de intentos de login burlado detrás del proxy | nginx toma la IP real de Cloudflare solo si viene del túnel y la reenvía sin dejar que el cliente la falsee |
| Claves en el repositorio | Todas en el `.env` del servidor y en secretos de GitHub; el `.env` está en `.gitignore` |
| Despliegue de código roto | Solo se publica lo que pasó compilación, pruebas y análisis de dependencias. Aprobación manual opcional |
| Suplantar al servidor durante el despliegue | La huella SSH del servidor se carga a mano; el despliegue no acepta otra |
| Imágenes base desactualizadas | Dependabot revisa cada semana los Dockerfile y el compose |
| Pérdida de datos | Respaldo diario de base y adjuntos, con retención y procedimiento de restauración |
| Correos tomados como spam o suplantados | SPF, DKIM y DMARC en el dominio |
| Disco lleno por logs | Logs de Docker con tope de 50 MB por contenedor |
