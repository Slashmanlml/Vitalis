import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProfesionalService } from '../services/profesional.service';
import { EspecialidadService } from '../services/especialidad.service';
import { UploadService } from '../services/upload.service';
import { Profesional, CrearProfesional, EditarProfesional } from '../models/profesional.model';
import { Especialidad } from '../models/especialidad.model';
import { ToastService } from '../services/toast.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-profesionales',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profesionales.html',
  styleUrls: ['./profesionales.css']
})
export class ProfesionalesComponent implements OnInit {
  serverUrl = environment.serverUrl;
  profesionales: Profesional[] = [];
  especialidades: Especialidad[] = [];
  showModal: boolean = false;
  editMode: boolean = false;
  selectedProfesional: Profesional | null = null;
  touched: { [key: string]: boolean } = {};

  form: CrearProfesional & EditarProfesional = {
    nombre: '', apellido: '', matricula: '', email: '', especialidadId: 0, fotoUrl: ''
  };

  constructor(
    private profesionalService: ProfesionalService,
    private especialidadService: EspecialidadService,
    private uploadService: UploadService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargarProfesionales();
    this.cargarEspecialidades();
  }

  cargarProfesionales() {
    this.profesionalService.obtenerTodos().subscribe(data => {
      this.profesionales = data;
      this.cdr.detectChanges();
    });
  }

  cargarEspecialidades() {
    this.especialidadService.obtenerTodas().subscribe(data => {
      this.especialidades = data;
      this.cdr.detectChanges();
    });
  }

  abrirNuevo() {
    this.editMode = false;
    this.selectedProfesional = null;
    this.form = { nombre: '', apellido: '', matricula: '', email: '', especialidadId: 0, fotoUrl: '' };
    this.touched = {};
    this.showModal = true;
  }

  abrirEditar(p: Profesional) {
    this.editMode = true;
    this.selectedProfesional = p;
    this.form = {
      nombre: p.nombre, apellido: p.apellido, matricula: p.matricula,
      email: p.email, especialidadId: p.especialidadId, fotoUrl: p.fotoUrl || ''
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
      case 'matricula': return !!this.form.matricula && /^[a-zA-Z0-9-\s]{4,15}$/.test(this.form.matricula);
      case 'email': return !!this.form.email && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email);
      case 'especialidadId': return !!this.form.especialidadId && this.form.especialidadId > 0;
      default: return true;
    }
  }

  markFieldTouched(field: string) {
    this.touched[field] = true;
  }

  isFormValid(): boolean {
    return this.isFieldValid('nombre') && 
           this.isFieldValid('apellido') && 
           this.isFieldValid('matricula') && 
           this.isFieldValid('email') &&
           this.isFieldValid('especialidadId');
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
      case 'matricula':
        if (!this.form.matricula) return 'La matrícula es requerida';
        if (!/^[a-zA-Z0-9-\s]{4,15}$/.test(this.form.matricula)) return 'La matrícula debe tener entre 4 y 15 caracteres alfanuméricos';
        return '';
      case 'email':
        if (!this.form.email) return 'El email es requerido';
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email)) return 'Email inválido';
        return '';
      case 'especialidadId':
        if (!this.form.especialidadId || this.form.especialidadId === 0) return 'Selecciona una especialidad';
        return '';
      default: return '';
    }
  }

  guardar() {
    // Mark all fields as touched for validation
    ['nombre', 'apellido', 'matricula', 'email', 'especialidadId'].forEach(field => {
      this.touched[field] = true;
    });

    if (!this.isFormValid()) {
      return;
    }

    if (this.editMode && this.selectedProfesional) {
      const dto: EditarProfesional = {
        nombre: this.form.nombre,
        apellido: this.form.apellido,
        matricula: this.form.matricula,
        email: this.form.email,
        especialidadId: this.form.especialidadId,
        fotoUrl: this.form.fotoUrl
      };
      this.profesionalService.editar(this.selectedProfesional.id, dto).subscribe(() => {
        this.toastService.success('Médico actualizado con éxito');
        this.cargarProfesionales();
        this.showModal = false;
      });
    } else {
      const dto: CrearProfesional = {
        nombre: this.form.nombre,
        apellido: this.form.apellido,
        matricula: this.form.matricula,
        email: this.form.email,
        especialidadId: this.form.especialidadId,
        fotoUrl: this.form.fotoUrl
      };
      this.profesionalService.crear(dto).subscribe(() => {
        this.toastService.success('Médico registrado con éxito');
        this.cargarProfesionales();
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

  eliminar(p: Profesional) {
    if (confirm(`¿Eliminar profesional ${p.nombre} ${p.apellido}?`)) {
      this.profesionalService.eliminar(p.id).subscribe(() => {
        this.toastService.success('Médico eliminado con éxito');
        this.cargarProfesionales();
      });
    }
  }

  cerrarModal() { this.showModal = false; }
}
