import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PrestacionService } from '../services/prestacion.service';
import { Prestacion } from '../models/prestacion.model';
import { puedeEditar } from '../utils/permisos';

@Component({
  selector: 'app-prestaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './prestaciones.html',
  styleUrls: ['./prestaciones.css']
})
export class PrestacionesComponent implements OnInit {
  puedeEditar = puedeEditar('prestaciones');
  prestaciones: Prestacion[] = [];
  showModal: boolean = false;
  editMode: boolean = false;
  selectedP: Prestacion | null = null;
  form: any = { nombre: '', codigo: '', importeBase: 0, activa: true };

  constructor(private service: PrestacionService, private cdr: ChangeDetectorRef) {}

  ngOnInit() { this.cargar(); }

  cargar() {
    this.service.obtenerTodas().subscribe(d => {
      this.prestaciones = d;
      this.cdr.detectChanges();
    });
  }

  abrirNuevo() { this.editMode = false; this.selectedP = null; this.form = { nombre: '', codigo: '', importeBase: 0, activa: true }; this.showModal = true; }

  abrirEditar(p: Prestacion) {
    this.editMode = true; this.selectedP = p;
    this.form = { nombre: p.nombre, codigo: p.codigo, importeBase: p.importeBase, activa: p.activa };
    this.showModal = true;
  }

  guardar() {
    if (this.editMode && this.selectedP) {
      this.service.editar(this.selectedP.id, this.form).subscribe(() => {
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

  eliminar(p: Prestacion) {
    if (confirm(`¿Eliminar ${p.nombre}?`)) {
      this.service.eliminar(p.id).subscribe(() => {
        this.cargar();
        this.cdr.detectChanges();
      });
    }
  }

  cerrarModal() { this.showModal = false; }
}
