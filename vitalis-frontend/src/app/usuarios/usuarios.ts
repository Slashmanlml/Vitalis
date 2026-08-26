import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UsuarioService } from '../services/usuario.service';
import { ToastService } from '../services/toast.service';
import { Usuario, CrearUsuario, EditarUsuario, ROLES_USUARIO } from '../models/usuario.model';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './usuarios.html',
  styleUrls: ['./usuarios.css']
})
export class UsuariosComponent implements OnInit {
  usuarios: Usuario[] = [];
  roles = [...ROLES_USUARIO];
  buscar: string = '';
  showModal: boolean = false;
  editMode: boolean = false;
  selectedUsuario: Usuario | null = null;
  touched: { [key: string]: boolean } = {};

  form = {
    nombre: '',
    apellido: '',
    email: '',
    password: '',
    rol: ''
  };

  constructor(private service: UsuarioService, private toastService: ToastService, private cdr: ChangeDetectorRef) {}

  ngOnInit() { this.cargar(); }

  cargar() {
    this.service.obtenerTodos(this.buscar).subscribe(data => {
      this.usuarios = data;
      this.cdr.detectChanges();
    });
  }

  filtrar() {
    this.cargar();
  }

  abrirNuevo() {
    this.editMode = false; this.selectedUsuario = null;
    this.form = { nombre: '', apellido: '', email: '', password: '', rol: 'Medico' };
    this.touched = {};
    this.showModal = true;
  }

  abrirEditar(u: Usuario) {
    this.editMode = true; this.selectedUsuario = u;
    this.form = { nombre: u.nombre, apellido: u.apellido, email: u.email, password: '', rol: u.rol };
    this.touched = {};
    this.showModal = true;
  }

  isFieldInvalid(field: string): boolean {
    return !!this.touched[field] && !this.isFieldValid(field);
  }

  isFieldValid(field: string): boolean {
    switch (field) {
      case 'nombre': return !!this.form.nombre && this.form.nombre.trim().length >= 2;
      case 'apellido': return !!this.form.apellido && this.form.apellido.trim().length >= 2;
      case 'email': return !!this.form.email && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email);
      case 'rol': return !!this.form.rol && this.roles.includes(this.form.rol as any);
      case 'password': return !this.editMode && !!this.form.password && this.form.password.length >= 6;
      default: return true;
    }
  }

  markFieldTouched(field: string) {
    this.touched[field] = true;
  }

  isFormValid(): boolean {
    return this.isFieldValid('nombre')
      && this.isFieldValid('apellido')
      && this.isFieldValid('email')
      && this.isFieldValid('rol')
      && this.isFieldValid('password');
  }

  getFieldError(field: string): string {
    if (!this.touched[field]) return '';

    switch (field) {
      case 'nombre':
        if (!this.form.nombre) return 'El nombre es requerido';
        if (this.form.nombre.trim().length < 2) return 'El nombre debe tener al menos 2 caracteres';
        return '';
      case 'apellido':
        if (!this.form.apellido) return 'El apellido es requerido';
        if (this.form.apellido.trim().length < 2) return 'El apellido debe tener al menos 2 caracteres';
        return '';
      case 'email':
        if (!this.form.email) return 'El email es requerido';
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email)) return 'Ingrese un email válido';
        return '';
      case 'rol':
        if (!this.form.rol) return 'El rol es requerido';
        return '';
      case 'password':
        if (!this.editMode && !this.form.password) return 'La contraseña es requerida';
        if (!this.editMode && this.form.password.length < 6) return 'La contraseña debe tener al menos 6 caracteres';
        return '';
      default: return '';
    }
  }

  guardar() {
    ['nombre', 'apellido', 'email', 'rol', 'password'].forEach(field => {
      this.touched[field] = true;
    });

    if (!this.isFormValid()) {
      return;
    }

    if (this.editMode && this.selectedUsuario) {
      const dto: EditarUsuario = {
        nombre: this.form.nombre, apellido: this.form.apellido, email: this.form.email, rol: this.form.rol
      };
      this.service.editar(this.selectedUsuario.id, dto).subscribe(() => {
        this.cargar();
        this.showModal = false;
        this.toastService.success('Usuario actualizado con éxito');
        this.cdr.detectChanges();
      });
    } else {
      const dto: CrearUsuario = {
        nombre: this.form.nombre, apellido: this.form.apellido, email: this.form.email,
        password: this.form.password, rol: this.form.rol
      };
      this.service.crear(dto).subscribe(() => {
        this.cargar();
        this.showModal = false;
        this.toastService.success('Usuario creado con éxito');
        this.cdr.detectChanges();
      });
    }
  }

  desactivar(u: Usuario) {
    if (!u.activo) return;
    const confirmacion = `¿Desactivar el usuario ${u.nombre} ${u.apellido}?\n\nSe trata de una baja lógica: dejará de poder acceder al sistema, pero no se borrará su registro.`;
    if (confirm(confirmacion)) {
      this.service.desactivar(u.id).subscribe(() => {
        this.cargar();
        this.toastService.success('Usuario desactivado correctamente');
        this.cdr.detectChanges();
      });
    }
  }

  cerrarModal() { this.showModal = false; }
}