import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, UsuarioPerfil } from '../services/auth.service';

@Component({
  selector: 'app-perfil',
  standalone: true,
  imports: [CommonModule, FormsModule],
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

  constructor(private authService: AuthService) {}

  ngOnInit() {
    this.authService.obtenerPerfil().subscribe({
      next: p => this.perfil = p,
      error: () => this.error = 'Error al cargar perfil'
    });
  }

  togglePasswordForm() {
    this.showPasswordForm = !this.showPasswordForm;
    this.passwordActual = '';
    this.passwordNuevo = '';
    this.passwordConfirmar = '';
    this.mensaje = '';
    this.error = '';
  }

  cambiarPassword() {
    if (this.passwordNuevo !== this.passwordConfirmar) {
      this.error = 'Las contraseñas nuevas no coinciden';
      return;
    }
    if (this.passwordNuevo.length < 6) {
      this.error = 'La contraseña debe tener al menos 6 caracteres';
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
        this.error = err.error?.message || 'Error al cambiar contraseña';
        this.cargando = false;
      }
    });
  }
}
