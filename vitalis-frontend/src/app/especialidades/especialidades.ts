import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EspecialidadService } from '../services/especialidad.service';
import { Especialidad, CrearEspecialidad, EditarEspecialidad } from '../models/especialidad.model';

@Component({
  selector: 'app-especialidades',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './especialidades.html',
  styleUrls: ['./especialidades.css']
})
export class EspecialidadesComponent implements OnInit {
  especialidades: Especialidad[] = [];
  showModal: boolean = false;
  editMode: boolean = false;
  selectedEsp: Especialidad | null = null;
  touched: { [key: string]: boolean } = {};

  form: CrearEspecialidad = { nombre: '', descripcion: '' };

  constructor(private service: EspecialidadService, private cdr: ChangeDetectorRef) {}

  ngOnInit() { this.cargar(); }

  cargar() {
    this.service.obtenerTodas().subscribe(data => {
      this.especialidades = data;
      this.cdr.detectChanges();
    });
  }

  abrirNuevo() {
    this.editMode = false; this.selectedEsp = null;
    this.form = { nombre: '', descripcion: '' };
    this.touched = {};
    this.showModal = true;
  }

  abrirEditar(e: Especialidad) {
    this.editMode = true; this.selectedEsp = e;
    this.form = { nombre: e.nombre, descripcion: e.descripcion || '' };
    this.touched = {};
    this.showModal = true;
  }

  isFieldInvalid(field: string): boolean {
    return !!this.touched[field] && !this.isFieldValid(field);
  }

  isFieldValid(field: string): boolean {
    switch (field) {
      case 'nombre': return !!this.form.nombre && this.form.nombre.trim().length >= 3;
      case 'descripcion': return !this.form.descripcion || this.form.descripcion.trim().length >= 5;
      default: return true;
    }
  }

  markFieldTouched(field: string) {
    this.touched[field] = true;
  }

  isFormValid(): boolean {
    return this.isFieldValid('nombre') && this.isFieldValid('descripcion');
  }

  getFieldError(field: string): string {
    if (!this.touched[field]) return '';
    
    switch (field) {
      case 'nombre':
        if (!this.form.nombre) return 'El nombre es requerido';
        if (this.form.nombre.trim().length < 3) return 'El nombre debe tener al menos 3 caracteres';
        return '';
      case 'descripcion':
        if (this.form.descripcion && this.form.descripcion.trim().length < 5) return 'La descripción debe tener al menos 5 caracteres';
        return '';
      default: return '';
    }
  }

  guardar() {
    // Mark all fields as touched for validation
    ['nombre', 'descripcion'].forEach(field => {
      this.touched[field] = true;
    });

    if (!this.isFormValid()) {
      return;
    }

    if (this.editMode && this.selectedEsp) {
      const dto: EditarEspecialidad = { ...this.form };
      this.service.editar(this.selectedEsp.id, dto).subscribe(() => {
        this.cargar();
        this.showModal = false;
        this.cdr.detectChanges();
      });
    } else {
      this.service.crear(this.form).subscribe(() => {
        this.cargar();
        this.showModal = false;
        this.cdr.detectChanges();
      });
    }
  }

  eliminar(e: Especialidad) {
    if (confirm(`¿Eliminar especialidad ${e.nombre}?`)) {
      this.service.eliminar(e.id).subscribe(() => {
        this.cargar();
        this.cdr.detectChanges();
      });
    }
  }

  cerrarModal() { this.showModal = false; }
}
