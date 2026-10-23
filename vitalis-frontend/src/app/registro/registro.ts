import { ChangeDetectionStrategy, Component, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService, RegistroPaciente } from '../services/auth.service';
import { esPasswordSegura, mensajeDeError } from '../utils/password';
import { PanelMarcaComponent } from '../shared/panel-marca';
import { CampoPasswordComponent } from '../shared/campo-password';
import { FuerzaPasswordComponent } from '../shared/fuerza-password';

type Campo = 'nombre' | 'apellido' | 'dni' | 'fechaNacimiento' | 'email' | 'password' | 'confirmarPassword' | 'aceptaTerminos';

/** Orden en que aparecen en pantalla: el primero con error es al que se lleva al usuario. */
const ORDEN: Campo[] = ['nombre', 'apellido', 'dni', 'fechaNacimiento', 'email', 'password', 'confirmarPassword', 'aceptaTerminos'];

/**
 * Registro de pacientes.
 *
 * Por qué se rehízo la validación: antes, si un dato no cumplía la regla, el
 * mensaje aparecía arriba de todo el formulario mientras la persona estaba
 * abajo, en el botón. No veía nada y parecía que la página se había colgado.
 * Ahora cada error aparece debajo de su campo, y al enviar se lleva la
 * pantalla al primer campo con problemas, se lo enfoca y el formulario hace
 * una sacudida breve.
 */
@Component({
  selector: 'app-registro',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CommonModule, FormsModule, RouterLink, PanelMarcaComponent, CampoPasswordComponent, FuerzaPasswordComponent],
  templateUrl: './registro.html',
  styleUrls: ['../shared/auth.css', './registro.css']
})
export class RegistroComponent {
  datos: RegistroPaciente = {
    nombre: '',
    apellido: '',
    dni: '',
    fechaNacimiento: '',
    email: '',
    telefono: '',
    password: '',
    confirmarPassword: '',
    aceptaTerminos: false
  };

  readonly hoy = new Date().toISOString().slice(0, 10);

  enviando = false;
  /** Error que devolvió el servidor (no de un campo puntual). */
  errorServidor = '';
  /** Mensaje genérico del backend una vez creado. */
  enviado = '';
  /** Se marcan los errores recién cuando la persona intenta enviar o sale del campo. */
  intentoEnviar = false;
  tocados = new Set<Campo>();
  sacudir = false;
  mostrarPassword = false;

  @ViewChild('tarjeta') private tarjeta?: ElementRef<HTMLElement>;
  @ViewChild('passwordCampo') private passwordCampo?: CampoPasswordComponent;

  constructor(private authService: AuthService, private host: ElementRef<HTMLElement>) {}

  /** Mensaje de error de cada campo, o '' si está bien. */
  errorDe(campo: Campo): string {
    const d = this.datos;
    switch (campo) {
      case 'nombre':
        return d.nombre.trim().length >= 2 ? '' : 'Escribí tu nombre.';
      case 'apellido':
        return d.apellido.trim().length >= 2 ? '' : 'Escribí tu apellido.';
      case 'dni': {
        const dni = d.dni.trim();
        if (!dni) return 'Escribí tu DNI.';
        if (/[.\s-]/.test(dni)) return 'Escribilo sin puntos ni espacios, por ejemplo 30111222.';
        return /^\d{7,8}$/.test(dni) ? '' : 'El DNI tiene 7 u 8 números.';
      }
      case 'fechaNacimiento':
        if (!d.fechaNacimiento) return 'Elegí tu fecha de nacimiento.';
        return d.fechaNacimiento > this.hoy || d.fechaNacimiento < '1900-01-01' ? 'Revisá la fecha: no puede ser futura.' : '';
      case 'email':
        if (!d.email.trim()) return 'Escribí tu correo: ahí te mandamos el enlace para activar la cuenta.';
        return /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(d.email.trim()) ? '' : 'Ese correo no parece válido. Revisá que tenga @ y un dominio, como nombre@gmail.com.';
      case 'password':
        if (!d.password) return 'Elegí una contraseña.';
        return esPasswordSegura(d.password) ? '' : 'Todavía no cumple los requisitos de la lista.';
      case 'confirmarPassword':
        if (!d.confirmarPassword) return 'Repetí la contraseña.';
        return d.confirmarPassword === d.password ? '' : 'No coincide con la contraseña de arriba.';
      case 'aceptaTerminos':
        return d.aceptaTerminos ? '' : 'Para crear la cuenta tenés que aceptar el tratamiento de tus datos.';
    }
  }

  /** ¿Mostrar el error de este campo ahora? */
  ver(campo: Campo): boolean {
    return (this.intentoEnviar || this.tocados.has(campo)) && !!this.errorDe(campo);
  }

  tocar(campo: Campo) {
    this.tocados.add(campo);
  }

  usarSugerida(password: string) {
    this.datos.password = password;
    this.datos.confirmarPassword = password;
    this.mostrarPassword = true;
    this.tocados.add('password');
    this.tocados.add('confirmarPassword');
  }

  registrar() {
    this.intentoEnviar = true;
    this.errorServidor = '';

    const primero = ORDEN.find(c => this.errorDe(c));
    if (primero) {
      this.llevarA(primero);
      return;
    }

    this.enviando = true;
    this.authService.registrar({
      ...this.datos,
      nombre: this.datos.nombre.trim(),
      apellido: this.datos.apellido.trim(),
      dni: this.datos.dni.trim(),
      email: this.datos.email.trim()
    }).subscribe({
      next: (r) => {
        this.enviando = false;
        this.enviado = r.mensaje;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      },
      error: (err) => {
        this.enviando = false;
        this.errorServidor = mensajeDeError(err, 'No pudimos crear la cuenta. Probá de nuevo en unos minutos.');
        // El aviso está arriba del formulario: se lleva la pantalla hasta ahí.
        setTimeout(() => {
          this.host.nativeElement.querySelector('#aviso-registro')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
          this.sacudirTarjeta();
        });
      }
    });
  }

  /** Lleva la pantalla al campo, lo enfoca y sacude el formulario. */
  private llevarA(campo: Campo) {
    setTimeout(() => {
      const raiz = this.host.nativeElement;
      const contenedor = raiz.querySelector<HTMLElement>(`[data-campo="${campo}"]`);
      contenedor?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      if (campo === 'password') {
        this.passwordCampo?.enfocar();
      } else {
        contenedor?.querySelector<HTMLElement>('input')?.focus({ preventScroll: true });
      }
      this.sacudirTarjeta();
    });
  }

  private sacudirTarjeta() {
    this.sacudir = false;
    requestAnimationFrame(() => (this.sacudir = true));
    setTimeout(() => (this.sacudir = false), 500);
  }

  get cantidadErrores(): number {
    return this.intentoEnviar ? ORDEN.filter(c => this.errorDe(c)).length : 0;
  }
}
