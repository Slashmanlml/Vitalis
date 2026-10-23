import { ChangeDetectionStrategy, Component, ElementRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { environment } from '../../environments/environment';
import { mensajeDeError } from '../utils/password';
import { ROL_PACIENTE } from '../guards/auth.guard';
import { PanelMarcaComponent } from '../shared/panel-marca';
import { CampoPasswordComponent } from '../shared/campo-password';

@Component({
  changeDetection: ChangeDetectionStrategy.Eager,
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PanelMarcaComponent, CampoPasswordComponent],
  templateUrl: './login.html',
  styleUrls: ['../shared/auth.css']
})
export class LoginComponent {
  email = '';
  password = '';
  errorMsg = '';
  infoMsg = '';
  isLoading = false;
  sacudir = false;
  intentoEnviar = false;

  /** El backend avisó que el correo no está confirmado: se ofrece reenviar el enlace. */
  puedeReenviar = false;
  reenviando = false;

  /** Las credenciales de prueba solo se muestran fuera de producción. */
  readonly mostrarCredencialesDemo = !environment.production;

  constructor(private authService: AuthService, private router: Router, private host: ElementRef<HTMLElement>) {}

  login() {
    this.intentoEnviar = true;
    this.errorMsg = '';
    this.infoMsg = '';
    this.puedeReenviar = false;

    if (!this.email.trim() || !this.password) {
      this.errorMsg = !this.email.trim() ? 'Escribí tu correo electrónico.' : 'Escribí tu contraseña.';
      this.avisar(!this.email.trim() ? '#email' : '#password');
      return;
    }

    this.isLoading = true;
    this.authService.login(this.email.trim(), this.password).subscribe({
      next: (res) => {
        this.isLoading = false;
        localStorage.setItem('token', res.token);
        this.router.navigate([res.rol === ROL_PACIENTE ? '/portal' : '/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        if (err?.error?.codigo === 'EMAIL_NO_CONFIRMADO') {
          this.puedeReenviar = true;
        }
        this.errorMsg = err?.status === 401
          ? 'El correo o la contraseña no son correctos.'
          : mensajeDeError(err, 'No se pudo iniciar sesión.');
        this.avisar('#password');
      }
    });
  }

  reenviar() {
    this.reenviando = true;
    this.authService.reenviarConfirmacion(this.email.trim()).subscribe({
      next: (r) => {
        this.reenviando = false;
        this.puedeReenviar = false;
        this.errorMsg = '';
        this.infoMsg = r.mensaje;
      },
      error: (err) => {
        this.reenviando = false;
        this.errorMsg = mensajeDeError(err, 'No se pudo reenviar el correo.');
      }
    });
  }

  /** Muestra el aviso: lleva la vista al formulario, enfoca el campo y lo sacude. */
  private avisar(selectorCampo: string) {
    setTimeout(() => {
      const raiz = this.host.nativeElement;
      raiz.querySelector('#aviso-login')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      raiz.querySelector<HTMLInputElement>(selectorCampo)?.focus({ preventScroll: true });
      this.sacudir = false;
      requestAnimationFrame(() => (this.sacudir = true));
      setTimeout(() => (this.sacudir = false), 500);
    });
  }
}
