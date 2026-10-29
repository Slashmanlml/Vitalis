# Correo real e integración continua

Dos cosas que dejan el sistema listo para mostrarse fuera de la máquina de
desarrollo: que los correos lleguen de verdad a una casilla, y que GitHub
compile y pruebe el proyecto solo en cada push.

## 1. Correo real (SMTP)

Por defecto los correos no salen (`Notificaciones:ModoPrueba = true`): se ven
en la pantalla **Notificaciones**. Para la demo conviene que lleguen de verdad,
pero todos a **tu** casilla, así no se le escribe a los pacientes inventados
de la semilla. Para eso está `RedirigirTodoA`: cada mail va a esa dirección y
el asunto dice para quién era.

La contraseña del correo **no va en ningún archivo del proyecto**: se guarda
con los *secretos de usuario* de .NET, que viven en tu perfil de Windows y no
se suben a GitHub.

### Con Gmail

1. Activá la verificación en dos pasos en tu cuenta de Google (es requisito).
2. Entrá a <https://myaccount.google.com/apppasswords>, creá una contraseña de
   aplicación llamada "Vitalis" y copiá los 16 caracteres que te muestra.
3. Guardá la configuración (reemplazá `tu.correo@gmail.com` y la clave):

   ```powershell
   cd C:\Users\Tito\Proyectos\Vitalis\vitalis\backend\src\Vitalis.Api
   dotnet user-secrets set "Notificaciones:Host" "smtp.gmail.com"
   dotnet user-secrets set "Notificaciones:Puerto" "587"
   dotnet user-secrets set "Notificaciones:Usuario" "tu.correo@gmail.com"
   dotnet user-secrets set "Notificaciones:Password" "la-clave-de-16-caracteres"
   dotnet user-secrets set "Notificaciones:RemitenteEmail" "tu.correo@gmail.com"
   dotnet user-secrets set "Notificaciones:RedirigirTodoA" "tu.correo@gmail.com"
   dotnet user-secrets set "Notificaciones:ModoPrueba" "false"
   ```

4. Reiniciá la API. En el arranque tiene que aparecer:
   `Correos: envío real por smtp.gmail.com:587, todos redirigidos a ...`
5. Probá: registrá un paciente nuevo y el correo de confirmación te llega a vos.

### Redirigir solo los correos de prueba

Con `RedirigirTodoA` solo, **todos** los correos van a tu casilla. Para que
cualquiera que se registre con su propio correo (por ejemplo, el profesor)
reciba el mail en su bandeja, sumá la lista de dominios de los datos de
prueba. Esos se siguen redirigiendo a vos y el resto llega a su dueño:

```powershell
cd C:\Users\Tito\Proyectos\Vitalis\vitalis\backend\src\Vitalis.Api
dotnet user-secrets set "Notificaciones:RedirigirSoloDominios" "email.com,vitalis.local"
```

Los pacientes de la semilla usan `@email.com`, que es un dominio real: por
eso no conviene sacar la redirección del todo.

Para volver al modo prueba:

```powershell
cd C:\Users\Tito\Proyectos\Vitalis\vitalis\backend\src\Vitalis.Api
dotnet user-secrets set "Notificaciones:ModoPrueba" "true"
```

Para ver lo que tenés guardado: `dotnet user-secrets list` (en la misma carpeta).

### Con Brevo (alternativa)

Cuenta gratuita en brevo.com → **SMTP & API** → generar una clave SMTP. Los
mismos comandos, con `Host` = `smtp-relay.brevo.com`, `Usuario` = el login
SMTP que muestra Brevo y `Password` = esa clave. El remitente tiene que ser
un correo verificado en Brevo.

### En Docker

Las mismas claves como variables de entorno en el `.env`
(`Notificaciones__Password=...`, con doble guion bajo). El `.env` está en
`.gitignore`.

## 2. Integración continua (GitHub Actions)

`.github/workflows/ci.yml` corre en cada push, en cualquier rama, y en cada
pull request. Tiene tres trabajos:

| Trabajo | Qué hace | Si falla |
| --- | --- | --- |
| Backend | Restaura, compila en Release, corre las pruebas automáticas y revisa paquetes NuGet con vulnerabilidades conocidas | Prueba rota o paquete vulnerable |
| Frontend | `npm ci`, pruebas, compilación de producción y `npm audit` de lo que llega al navegador | Prueba rota, error de compilación o dependencia con vulnerabilidad alta |
| Imágenes Docker | Construye las imágenes de la API y del frontend, sin publicarlas | El `docker compose` de despliegue dejó de funcionar |

Los resultados se ven en la pestaña **Actions** del repositorio. Un tilde verde
al lado del commit significa que todo pasó; una cruz roja, que algo se rompió.

`.github/dependabot.yml` revisa una vez por semana si hay versiones nuevas de
paquetes NuGet y npm (y una vez por mes de las acciones de GitHub) y abre un
pull request por cada una. Como la CI corre sobre ese pull request, se ve
enseguida si actualizar rompe algo.

Para la defensa: esto es la parte "Sec" de DevSecOps. Cada cambio pasa por
pruebas y por un control de dependencias vulnerables antes de llegar a `main`.

### Ponerlo en marcha

Alcanza con subir la carpeta `.github` junto con el resto del código. La
primera corrida empieza sola con ese push.
