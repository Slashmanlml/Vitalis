import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ObraSocialService } from '../services/obra-social.service';
import { ObraSocial, CrearObraSocial, EditarObraSocial } from '../models/obra-social.model';
import { puedeEditar } from '../utils/permisos';

@Component({
  selector: 'app-obras-sociales',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './obras-sociales.html',
  styleUrls: ['./obras-sociales.css']
})
export class ObrasSocialesComponent implements OnInit {
  puedeEditar = puedeEditar('obras-sociales');
  obrasSociales: ObraSocial[] = [];
  showModal: boolean = false;
  editMode: boolean = false;
  selectedOs: ObraSocial | null = null;
  touched: { [key: string]: boolean } = {};

  form: CrearObraSocial & { activa: boolean } = {
    nombre: '', codigo: '', activa: true
  };

  constructor(private service: ObraSocialService, private cdr: ChangeDetectorRef) {}

  ngOnInit() { this.cargar(); }

  cargar() {
    this.service.obtenerTodas().subscribe(data => {
      this.obrasSociales = data;
      this.cdr.detectChanges();
    });
  }

  abrirNuevo() {
    this.editMode = false; this.selectedOs = null;
    this.form = { nombre: '', codigo: '', activa: true };
    this.touched = {};
    this.showModal = true;
  }

  abrirEditar(os: ObraSocial) {
    this.editMode = true; this.selectedOs = os;
    this.form = { nombre: os.nombre, codigo: os.codigo, activa: os.activa };
    this.touched = {};
    this.showModal = true;
  }

  isFieldInvalid(field: string): boolean {
    return !!this.touched[field] && !this.isFieldValid(field);
  }

  isFieldValid(field: string): boolean {
    switch (field) {
      case 'nombre': return !!this.form.nombre && this.form.nombre.trim().length >= 2;
      case 'codigo': return !!this.form.codigo && /^[A-Z0-9]{2,10}$/.test(this.form.codigo);
      default: return true;
    }
  }

  markFieldTouched(field: string) {
    this.touched[field] = true;
  }

  isFormValid(): boolean {
    return this.isFieldValid('nombre') && this.isFieldValid('codigo');
  }

  getFieldError(field: string): string {
    if (!this.touched[field]) return '';
    
    switch (field) {
      case 'nombre':
        if (!this.form.nombre) return 'El nombre es requerido';
        if (this.form.nombre.trim().length < 2) return 'El nombre debe tener al menos 2 caracteres';
        return '';
      case 'codigo':
        if (!this.form.codigo) return 'El código es requerido';
        if (!/^[A-Z0-9]{2,10}$/.test(this.form.codigo)) return 'Código: mayúsculas y números, 2-10 caracteres';
        return '';
      default: return '';
    }
  }

  guardar() {
    // Mark all fields as touched for validation
    ['nombre', 'codigo'].forEach(field => {
      this.touched[field] = true;
    });

    if (!this.isFormValid()) {
      return;
    }

    if (this.editMode && this.selectedOs) {
      const dto: EditarObraSocial = { nombre: this.form.nombre, codigo: this.form.codigo, activa: this.form.activa };
      this.service.editar(this.selectedOs.id, dto).subscribe(() => {
        this.cargar();
        this.showModal = false;
        this.cdr.detectChanges();
      });
    } else {
      const dto: CrearObraSocial = { nombre: this.form.nombre, codigo: this.form.codigo };
      this.service.crear(dto).subscribe(() => {
        this.cargar();
        this.showModal = false;
        this.cdr.detectChanges();
      });
    }
  }

  eliminar(os: ObraSocial) {
    if (confirm(`¿Eliminar obra social ${os.nombre}?`)) {
      this.service.eliminar(os.id).subscribe(() => {
        this.cargar();
        this.cdr.detectChanges();
      });
    }
  }

  cerrarModal() { this.showModal = false; }
}
