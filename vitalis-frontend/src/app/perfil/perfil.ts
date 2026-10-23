import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, UsuarioPerfil } from '../services/auth.service';
import { esPasswordSegura, DESCRIPCION_PASSWORD } from '../utils/password';
import { CampoPasswordComponent } from '../shared/campo-password';
import { FuerzaPasswordComponent } from '../shared/fuerza-password';

@Component({
  // Angular 22 usa OnPush por defecto. Esta pantalla guarda lo que llega del
  // servidor en propiedades comunes (no signals), así que necesita la
  // detección de cambios "de siempre" para que se dibuje al llegar la respuesta.
  changeDetection: ChangeDetectionStrategy.Eager,
  selector: 'app-perfil',
  standalone: true,
  imports: [CommonModule, FormsModule, CampoPasswordComponent, FuerzaPasswordComponent],
  templateUrl: './perfil.html',
  styleUrls: ['./perfil.css']
})
export class PerfilComponent implements OnInit {
  perfil: UsuarioPerfil | null = null;
  showPasswordForm = false;
  passwordActual = '';
  passwordNuevo = '';
  passwordConfirmar = '';
  mensaje = '';
  error = '';
  cargando = false;
  verNuevo = false;

  constructor(private authService: AuthService) {}

  ngOnInit() {
    this.authService.obtenerPerfil().subscribe({
      next: p => this.perfil = p,
      error: () => this.error = 'Error al cargar perfil'
    });
  }

  usarSugerida(p: string) {
    this.passwordNuevo = p;
    this.passwordConfirmar = p;
    this.verNuevo = true;
  }

  togglePasswordForm() {
    this.showPasswordForm = !this.showPasswordForm;
    this.passwordActual = '';
    this.passwordNuevo = '';
    this.passwordConfirmar = '';
    this.mensaje = '';
    this.error = '';
    this.verNuevo = false;
  }

  cambiarPassword() {
    if (this.passwordNuevo !== this.passwordConfirmar) {
      this.error = 'Las contraseñas nuevas no coinciden';
      return;
    }
    if (!esPasswordSegura(this.passwordNuevo)) {
      this.error = DESCRIPCION_PASSWORD;
      return;
    }
    this.cargando = true;
    this.error = '';
    this.mensaje = '';
    this.authService.cambiarPassword(this.passwordActual, this.passwordNuevo).subscribe({
      next: () => {
        this.mensaje = 'Contraseña actualizada correctamente.';
        this.cargando = false;
        this.passwordActual = '';
        this.passwordNuevo = '';
        this.passwordConfirmar = '';
        this.showPasswordForm = false;
      },
      error: (err) => {
        this.error = err.error?.mensaje || err.error?.message || 'Error al cambiar contraseña';
        this.cargando = false;
      }
    });
  }
}
