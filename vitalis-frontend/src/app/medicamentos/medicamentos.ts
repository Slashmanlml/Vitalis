import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MedicamentoService } from '../services/medicamento.service';
import { Medicamento, CrearMedicamento, EditarMedicamento } from '../models/medicamento.model';
import { puedeEditar } from '../utils/permisos';

@Component({
  selector: 'app-medicamentos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './medicamentos.html',
  styleUrls: ['./medicamentos.css']
})
export class MedicamentosComponent implements OnInit {
  puedeEditar = puedeEditar('medicamentos');
  medicamentos: Medicamento[] = [];
  searchTerm: string = '';
  showModal: boolean = false;
  editMode: boolean = false;
  selectedMed: Medicamento | null = null;
  form: CrearMedicamento = { nombre: '', presentacion: '' };

  constructor(private service: MedicamentoService, private cdr: ChangeDetectorRef) {}

  ngOnInit() { this.cargar(); }

  cargar() {
    this.service.obtenerTodos(this.searchTerm).subscribe(d => {
      this.medicamentos = d;
      this.cdr.detectChanges();
    });
  }

  buscar() { this.cargar(); }

  abrirNuevo() { this.editMode = false; this.selectedMed = null; this.form = { nombre: '', presentacion: '' }; this.showModal = true; }

  abrirEditar(m: Medicamento) {
    this.editMode = true; this.selectedMed = m;
    this.form = { nombre: m.nombre, presentacion: m.presentacion || '' };
    this.showModal = true;
  }

  guardar() {
    if (this.editMode && this.selectedMed) {
      this.service.editar(this.selectedMed.id, { ...this.form, activo: true }).subscribe(() => {
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

  eliminar(m: Medicamento) {
    if (confirm(`¿Eliminar ${m.nombre}?`)) {
      this.service.eliminar(m.id).subscribe(() => {
        this.cargar();
        this.cdr.detectChanges();
      });
    }
  }

  cerrarModal() { this.showModal = false; }
}
