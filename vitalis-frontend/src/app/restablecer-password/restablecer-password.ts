import { ChangeDetectionStrategy, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { DESCRIPCION_PASSWORD, esPasswordSegura, mensajeDeError } from '../utils/password';
import { PanelMarcaComponent } from '../shared/panel-marca';
import { CampoPasswordComponent } from '../shared/campo-password';
import { FuerzaPasswordComponent } from '../shared/fuerza-password';

type Estado = 'formulario' | 'ok' | 'sin-token';
type Campo = 'password' | 'confirmar';

/**
 * Destino del enlace del correo: /restablecer-password?token=...
 *
 * Igual que en confirmar-email, el token se guarda en memoria y se borra de
 * la barra de direcciones apenas se lee, para que no quede en el historial.
 * Recién se manda al backend cuando la persona elige la contraseña nueva.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.Eager,
  selector: 'app-restablecer-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PanelMarcaComponent, CampoPasswordComponent, FuerzaPasswordComponent],
  templateUrl: './restablecer-password.html',
  styleUrls: ['../shared/auth.css', '../olvide-password/olvide-password.css']
})
export class RestablecerPasswordComponent implements OnInit {
  estado: Estado = 'formulario';
  password = '';
  confirmar = '';
  mostrarPassword = false;
  enviando = false;
  intentoEnviar = false;
  errorMsg = '';
  mensaje = '';
  sacudir = false;
  /** El enlace venció o ya se usó: se ofrece pedir otro en vez de reintentar. */
  enlaceInvalido = false;

  private token = '';
  private tocados = new Set<Campo>();

  @ViewChild('passwordCampo') private passwordCampo?: CampoPasswordComponent;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private host: ElementRef<HTMLElement>
  ) {}

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.router.navigate([], { replaceUrl: true, queryParams: {} });
    if (!this.token) this.estado = 'sin-token';
  }

  errorDe(campo: Campo): string {
    if (campo === 'password') {
      if (!this.password) return 'Elegí una contraseña nueva.';
      if (!esPasswordSegura(this.password)) return DESCRIPCION_PASSWORD;
      return '';
    }
    if (!this.confirmar) return 'Repetí la contraseña.';
    if (this.confirmar !== this.password) return 'Las dos contraseñas no coinciden.';
    return '';
  }

  ver(campo: Campo): boolean {
    return (this.intentoEnviar || this.tocados.has(campo)) && !!this.errorDe(campo);
  }

  tocar(campo: Campo) {
    this.tocados.add(campo);
  }

  usarSugerida(password: string) {
    this.password = password;
    this.confirmar = password;
    this.mostrarPassword = true;
    this.tocados.add('password');
    this.tocados.add('confirmar');
  }

  guardar() {
    this.intentoEnviar = true;
    this.errorMsg = '';
    const primero = (['password', 'confirmar'] as Campo[]).find(c => this.errorDe(c));
    if (primero) {
      this.llevarA(primero);
      return;
    }

    this.enviando = true;
    this.authService.restablecerPassword(this.token, this.password, this.confirmar).subscribe({
      next: (r) => {
        this.enviando = false;
        this.estado = 'ok';
        this.mensaje = r.mensaje;
        this.password = this.confirmar = '';
      },
      error: (err) => {
        this.enviando = false;
        // 400 sin errores por campo = el enlace no sirve (vencido, usado o adulterado).
        this.enlaceInvalido = err?.status === 400 && !err?.error?.errors;
        this.errorMsg = mensajeDeError(err, 'No pudimos cambiar la contraseña.');
        this.llevarA('password');
      }
    });
  }

  private llevarA(campo: Campo) {
    setTimeout(() => {
      const raiz = this.host.nativeElement;
      const destino = raiz.querySelector<HTMLElement>(this.errorMsg ? '#aviso-restablecer' : `[data-campo="${campo}"]`);
      destino?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      if (campo === 'password') this.passwordCampo?.enfocar();
      else raiz.querySelector<HTMLInputElement>('#confirmar')?.focus({ preventScroll: true });
      this.sacudir = false;
      requestAnimationFrame(() => (this.sacudir = true));
      setTimeout(() => (this.sacudir = false), 500);
    });
  }
}
