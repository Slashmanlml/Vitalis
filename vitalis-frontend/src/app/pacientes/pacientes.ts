import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PacienteService } from '../services/paciente.service';
import { ObraSocialService } from '../services/obra-social.service';
import { UploadService } from '../services/upload.service';
import { Paciente, CrearPaciente, EditarPaciente } from '../models/paciente.model';
import { ObraSocial } from '../models/obra-social.model';
import { ToastService } from '../services/toast.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-pacientes',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './pacientes.html',
  styleUrls: ['./pacientes.css']
})
export class PacientesComponent implements OnInit {
  serverUrl = environment.serverUrl;
  pacientes: Paciente[] = [];
  filteredPacientes: Paciente[] = [];
  obrasSociales: ObraSocial[] = [];
  searchTerm: string = '';
  showModal: boolean = false;
  editMode: boolean = false;
  selectedPaciente: Paciente | null = null;
  touched: { [key: string]: boolean } = {};

  form: CrearPaciente & EditarPaciente = {
    nombre: '', apellido: '', dni: '', fechaNacimiento: '', email: '',
    telefono: '', direccion: '', obraSocialId: undefined, numeroAfiliado: '',
    fotoUrl: ''
  };

  constructor(
    private router: Router,
    private pacienteService: PacienteService,
    private obraSocialService: ObraSocialService,
    private uploadService: UploadService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargarPacientes();
    this.cargarObrasSociales();
  }

  cargarPacientes() {
    this.pacienteService.obtenerTodos().subscribe(data => {
      this.pacientes = data;
      this.filteredPacientes = data;
      this.cdr.detectChanges();
    });
  }

  cargarObrasSociales() {
    this.obraSocialService.obtenerTodas().subscribe(data => {
      this.obrasSociales = data;
      this.cdr.detectChanges();
    });
  }

  buscar() {
    this.pacienteService.obtenerTodos(this.searchTerm).subscribe(data => {
      this.pacientes = data;
      this.filteredPacientes = data;
      this.cdr.detectChanges();
    });
  }

  abrirNuevo() {
    this.editMode = false;
    this.selectedPaciente = null;
    this.form = { nombre: '', apellido: '', dni: '', fechaNacimiento: '', email: '', telefono: '', direccion: '', obraSocialId: undefined, numeroAfiliado: '', fotoUrl: '' };
    this.touched = {};
    this.showModal = true;
  }

  abrirEditar(p: Paciente) {
    this.editMode = true;
    this.selectedPaciente = p;
    this.form = {
      nombre: p.nombre, apellido: p.apellido, dni: p.dni,
      fechaNacimiento: p.fechaNacimiento ? p.fechaNacimiento.substring(0, 10) : '',
      email: p.email || '', telefono: p.telefono || '',
      direccion: p.direccion || '', obraSocialId: p.obraSocialId,
      numeroAfiliado: p.numeroAfiliado || '',
      fotoUrl: p.fotoUrl || ''
    };
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
      case 'dni': {
        if (!this.form.dni) return false;
        const cleanDni = this.form.dni.replace(/\./g, '');
        return /^\d{6,9}$/.test(cleanDni);
      }
      case 'fechaNacimiento': return !!this.form.fechaNacimiento;
      case 'email': return !this.form.email || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email);
      case 'telefono': {
        if (!this.form.telefono) return true;
        const validChars = /^[+0-9\s()-]*$/.test(this.form.telefono);
        if (!validChars) return false;
        const cleanTel = this.form.telefono.replace(/[^\d]/g, '');
        return cleanTel.length >= 6 && cleanTel.length <= 15;
      }
      default: return true;
    }
  }

  markFieldTouched(field: string) {
    this.touched[field] = true;
  }

  isFormValid(): boolean {
    return this.isFieldValid('nombre') && 
           this.isFieldValid('apellido') && 
           this.isFieldValid('dni') && 
           this.isFieldValid('fechaNacimiento') &&
           this.isFieldValid('email') &&
           this.isFieldValid('telefono');
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
      case 'dni': {
        if (!this.form.dni) return 'El DNI es requerido';
        const cleanDni = this.form.dni.replace(/\./g, '');
        if (!/^\d{6,9}$/.test(cleanDni)) return 'El DNI debe tener entre 6 y 9 dígitos';
        return '';
      }
      case 'fechaNacimiento':
        if (!this.form.fechaNacimiento) return 'La fecha de nacimiento es requerida';
        return '';
      case 'email':
        if (this.form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email)) return 'Email inválido';
        return '';
      case 'telefono': {
        if (this.form.telefono) {
          const validChars = /^[+0-9\s()-]*$/.test(this.form.telefono);
          if (!validChars) return 'El teléfono contiene caracteres no permitidos';
          const cleanTel = this.form.telefono.replace(/[^\d]/g, '');
          if (cleanTel.length < 6 || cleanTel.length > 15) return 'El teléfono debe tener entre 6 y 15 dígitos';
        }
        return '';
      }
      default: return '';
    }
  }

  guardar() {
    // Mark all fields as touched for validation
    ['nombre', 'apellido', 'dni', 'fechaNacimiento', 'email', 'telefono'].forEach(field => {
      this.touched[field] = true;
    });

    if (!this.isFormValid()) {
      return;
    }

    if (this.editMode && this.selectedPaciente) {
      const dto: EditarPaciente = {
        nombre: this.form.nombre, apellido: this.form.apellido,
        telefono: this.form.telefono, email: this.form.email,
        direccion: this.form.direccion, obraSocialId: this.form.obraSocialId,
        numeroAfiliado: this.form.numeroAfiliado,
        fotoUrl: this.form.fotoUrl
      };
      this.pacienteService.editar(this.selectedPaciente.id, dto).subscribe(() => {
        this.toastService.success('Datos del paciente actualizados con éxito');
        this.cargarPacientes();
        this.showModal = false;
      });
    } else {
      const dto: CrearPaciente = {
        nombre: this.form.nombre, apellido: this.form.apellido,
        dni: this.form.dni.replace(/\./g, ''),
        fechaNacimiento: this.form.fechaNacimiento,
        email: this.form.email || undefined, telefono: this.form.telefono,
        direccion: this.form.direccion, obraSocialId: this.form.obraSocialId,
        numeroAfiliado: this.form.numeroAfiliado,
        fotoUrl: this.form.fotoUrl
      };
      this.pacienteService.crear(dto).subscribe(() => {
        this.toastService.success('Paciente registrado con éxito');
        this.cargarPacientes();
        this.showModal = false;
      });
    }
  }

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (file) {
      this.uploadService.subirImagen(file).subscribe({
        next: (res) => {
          this.form.fotoUrl = res.url;
          this.toastService.success('Imagen de perfil subida correctamente');
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.toastService.error('Error al subir la imagen');
        }
      });
    }
  }

  desactivar(p: Paciente) {
    if (confirm(`¿Desactivar paciente ${p.nombre} ${p.apellido}?`)) {
      this.pacienteService.desactivar(p.id).subscribe(() => {
        this.toastService.success('Paciente desactivado correctamente');
        this.cargarPacientes();
      });
    }
  }

  verFicha(p: Paciente) {
    this.router.navigate(['/dashboard/pacientes', p.id]);
  }

  cerrarModal() {
    this.showModal = false;
  }
}
