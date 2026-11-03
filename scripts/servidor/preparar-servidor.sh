#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Deja un servidor Ubuntu 24.04 recién creado listo para Vitalis.
#
# Se corre UNA vez, como root, en el servidor nuevo:
#
#   bash preparar-servidor.sh
#
# Requisito: haber entrado al servidor con una clave SSH (no con contraseña).
# El script copia esa misma clave al usuario nuevo y recién después apaga el
# ingreso con contraseña y el de root. Si no encuentra la clave, se detiene
# antes de tocar SSH, para que nunca quedes afuera del servidor.
#
# Qué hace:
#   1. Actualiza el sistema y activa las actualizaciones de seguridad
#      automáticas.
#   2. Crea el usuario "vitalis" (con sudo) y le copia la clave SSH de root.
#   3. SSH: solo con clave, sin root, sin contraseñas.
#   4. Firewall (ufw): rechaza todo lo entrante salvo SSH. El sitio no necesita
#      puertos abiertos porque entra por el túnel de Cloudflare.
#   5. fail2ban: bloquea por un rato las IP que fallan muchas veces en SSH.
#   6. Instala Docker desde el repositorio oficial.
#   7. Crea /opt/vitalis y 2 GB de memoria de intercambio (swap).
#
# Se puede volver a correr sin romper nada: cada paso revisa si ya está hecho.
# ---------------------------------------------------------------------------
set -euo pipefail

USUARIO="${USUARIO:-vitalis}"
CARPETA="/opt/vitalis"

paso() { printf '\n\033[1;36m==> %s\033[0m\n' "$1"; }
falla() { printf '\n\033[1;31mERROR: %s\033[0m\n' "$1" >&2; exit 1; }

[[ $EUID -eq 0 ]] || falla "Correlo como root (o con sudo)."
grep -q 'VERSION_ID="24.04"' /etc/os-release \
  || echo "Aviso: está pensado para Ubuntu 24.04; en otra versión revisá cada paso."

# --- 0. Antes de tocar nada: ¿hay una clave SSH para copiar? ---------------
CLAVES_ROOT="/root/.ssh/authorized_keys"
if [[ ! -s "$CLAVES_ROOT" ]] && [[ ! -s "/home/$USUARIO/.ssh/authorized_keys" ]]; then
  falla "No hay ninguna clave SSH en $CLAVES_ROOT. Creá el servidor con tu clave pública (o agregala ahí) y volvé a correr el script. No se tocó nada."
fi

# --- 1. Sistema al día ------------------------------------------------------
paso "Actualizando el sistema"
export DEBIAN_FRONTEND=noninteractive
apt-get update -q
apt-get upgrade -y -q
apt-get install -y -q ca-certificates curl gnupg ufw fail2ban unattended-upgrades

paso "Actualizaciones de seguridad automáticas"
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'EOF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
APT::Periodic::AutocleanInterval "7";
EOF

# --- 2. Usuario sin root ----------------------------------------------------
paso "Usuario $USUARIO"
if ! id "$USUARIO" &>/dev/null; then
  adduser --disabled-password --gecos "" "$USUARIO"
fi
usermod -aG sudo "$USUARIO"
install -d -m 700 -o "$USUARIO" -g "$USUARIO" "/home/$USUARIO/.ssh"
if [[ -s "$CLAVES_ROOT" ]]; then
  # Se agregan las claves de root sin duplicar las que ya estuvieran.
  touch "/home/$USUARIO/.ssh/authorized_keys"
  sort -u "$CLAVES_ROOT" "/home/$USUARIO/.ssh/authorized_keys" -o "/home/$USUARIO/.ssh/authorized_keys"
fi
chown "$USUARIO:$USUARIO" "/home/$USUARIO/.ssh/authorized_keys"
chmod 600 "/home/$USUARIO/.ssh/authorized_keys"

# sudo pide contraseña; como el usuario no tiene, se le pide al operador que
# defina una. Sin esto, el usuario podría entrar pero no administrar.
if ! passwd -S "$USUARIO" | grep -q ' P '; then
  echo
  echo "Definí una contraseña para '$USUARIO'. Se usa SOLO para sudo (para entrar"
  echo "por SSH se sigue usando la clave). Guardala en Bitwarden."
  passwd "$USUARIO"
fi

# --- 3. SSH -----------------------------------------------------------------
paso "Endureciendo SSH"
# El nombre empieza con 10- a propósito: sshd se queda con el PRIMER valor que
# lee, y los proveedores suelen dejar un 50-cloud-init.conf que habilita las
# contraseñas. Leído antes, este manda.
cat > /etc/ssh/sshd_config.d/10-vitalis.conf <<EOF
# Generado por preparar-servidor.sh
PermitRootLogin no
PasswordAuthentication no
KbdInteractiveAuthentication no
PubkeyAuthentication yes
MaxAuthTries 3
LoginGraceTime 30
X11Forwarding no
AllowUsers $USUARIO
EOF
# Si la configuración tiene un error, sshd -t lo dice y no se reinicia: un
# SSH roto con la sesión cerrada es quedarse sin servidor.
sshd -t || { rm -f /etc/ssh/sshd_config.d/10-vitalis.conf; falla "La configuración de SSH no es válida; se deshizo el cambio."; }
systemctl reload ssh || systemctl restart ssh

# --- 4. Firewall ------------------------------------------------------------
paso "Firewall (ufw)"
ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw --force enable
# Nota: Docker escribe sus propias reglas y un puerto publicado con "ports:"
# se saltea ufw. Por eso docker-compose.prod.yml no publica ninguno.

# --- 5. fail2ban ------------------------------------------------------------
paso "fail2ban para SSH"
cat > /etc/fail2ban/jail.d/vitalis.local <<'EOF'
[sshd]
enabled  = true
backend  = systemd
maxretry = 5
findtime = 10m
bantime  = 1h
EOF
systemctl enable --now fail2ban
systemctl restart fail2ban

# --- 6. Docker --------------------------------------------------------------
paso "Docker"
if ! command -v docker &>/dev/null; then
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc
  # shellcheck disable=SC1091
  . /etc/os-release
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable" \
    > /etc/apt/sources.list.d/docker.list
  apt-get update -q
  apt-get install -y -q docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi
systemctl enable --now docker
# Estar en el grupo docker equivale a ser root en esta máquina. Se acepta
# porque el usuario ya tiene sudo y lo necesita el despliegue automático.
usermod -aG docker "$USUARIO"

# --- 7. Carpeta y swap ------------------------------------------------------
paso "Carpeta $CARPETA"
install -d -m 750 -o "$USUARIO" -g "$USUARIO" "$CARPETA" "$CARPETA/respaldos"

paso "Memoria de intercambio"
if ! swapon --show | grep -q .; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  grep -q '^/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
else
  echo "Ya hay swap; no se toca."
fi

paso "Listo"
cat <<EOF

El servidor está preparado. ANTES de cerrar esta sesión, abrí otra terminal
y comprobá que podés entrar con el usuario nuevo:

    ssh $USUARIO@$(hostname -I | awk '{print $1}')

Si entra, ya podés cerrar esta. Desde ahora root no puede entrar por SSH.
Lo que sigue está en docs/24-despliegue.md (copiar .env y levantar).
EOF
