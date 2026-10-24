import { ChangeDetectionStrategy, Component, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { mensajeDeError } from '../utils/password';
import { PanelMarcaComponent } from '../shared/panel-marca';

const EMAIL_VALIDO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * "Olvidé mi contraseña": pide el correo y manda el enlace.
 *
 * La respuesta del backend es la misma exista o no la cuenta, así que esta
 * pantalla tampoco lo distingue: siempre muestra "si el correo está
 * registrado, te llega un enlace". Así nadie puede usarla para averiguar
 * quién es paciente del consultorio.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.Eager,
  selector: 'app-olvide-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PanelMarcaComponent],
  templateUrl: './olvide-password.html',
  styleUrls: ['../shared/auth.css', './olvide-password.css']
})
export class OlvidePasswordComponent {
  email = '';
  enviando = false;
  enviado = false;
  intentoEnviar = false;
  errorMsg = '';
  mensaje = '';
  sacudir = false;

  constructor(private authService: AuthService, private host: ElementRef<HTMLElement>) {
    // Si viene del login con el correo ya escrito, se aprovecha. Viaja en el
    // estado de la navegación y no en la URL, para que no quede en el
    // historial ni en los registros del servidor.
    const desdeLogin = (globalThis.history?.state as { email?: unknown } | null)?.email;
    this.email = typeof desdeLogin === 'string' ? desdeLogin : '';
  }

  get errorEmail(): string {
    const e = this.email.trim();
    if (!e) return 'Escribí tu correo electrónico.';
    if (!EMAIL_VALIDO.test(e)) return 'Revisá el correo: falta algo (por ejemplo, la @ o el dominio).';
    return '';
  }

  enviar() {
    this.intentoEnviar = true;
    this.errorMsg = '';
    if (this.errorEmail) {
      this.avisar();
      return;
    }

    this.enviando = true;
    this.authService.olvidePassword(this.email.trim()).subscribe({
      next: (r) => {
        this.enviando = false;
        this.enviado = true;
        this.mensaje = r.mensaje;
      },
      error: (err) => {
        this.enviando = false;
        this.errorMsg = mensajeDeError(err, 'No pudimos mandar el correo. Probá de nuevo en un momento.');
        this.avisar();
      }
    });
  }

  otraVez() {
    this.enviado = false;
    this.intentoEnviar = false;
  }

  private avisar() {
    setTimeout(() => {
      const campo = this.host.nativeElement.querySelector<HTMLInputElement>('#email');
      campo?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      campo?.focus({ preventScroll: true });
      this.sacudir = false;
      requestAnimationFrame(() => (this.sacudir = true));
      setTimeout(() => (this.sacudir = false), 500);
    });
  }
}
