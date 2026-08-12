import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LiquidacionService } from '../services/liquidacion.service';
import { ProfesionalService } from '../services/profesional.service';
import { Liquidacion, CrearLiquidacion } from '../models/liquidacion.model';
import { Profesional } from '../models/profesional.model';

@Component({
  selector: 'app-liquidaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './liquidaciones.html',
  styleUrls: ['./liquidaciones.css']
})
export class LiquidacionesComponent implements OnInit {
  liquidaciones: Liquidacion[] = [];
  profesionales: Profesional[] = [];
  filtroEstado: string = 'todas';
  showModal: boolean = false;
  form: CrearLiquidacion = { profesionalId: 0, periodoDesde: '', periodoHasta: '' };

  constructor(
    private service: LiquidacionService,
    private profesionalService: ProfesionalService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargar();
    this.profesionalService.obtenerTodos().subscribe(d => {
      this.profesionales = d;
      this.cdr.detectChanges();
    });
  }

  get filtered() {
    if (this.filtroEstado === 'todas') return this.liquidaciones;
    return this.liquidaciones.filter(l => l.estado === this.filtroEstado);
  }

  cargar() {
    this.service.obtenerTodas().subscribe(d => {
      this.liquidaciones = d;
      this.cdr.detectChanges();
    });
  }

  abrirModal() {
    const hoy = new Date();
    const inicio = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
    this.form = {
      profesionalId: 0,
      periodoDesde: inicio.toISOString().split('T')[0],
      periodoHasta: hoy.toISOString().split('T')[0]
    };
    this.showModal = true;
  }

  guardar() {
    this.service.crear(this.form).subscribe(() => {
      this.cargar();
      this.showModal = false;
      this.cdr.detectChanges();
    });
  }

  liquidar(l: Liquidacion) {
    this.service.liquidar(l.id).subscribe(() => {
      this.cargar();
      this.cdr.detectChanges();
    });
  }

  getEstadoClass(e: string): string {
    const map: any = { 'Liquidada': 'status-success', 'Pendiente': 'status-warning' };
    return map[e] || 'status-pending';
  }

  cerrarModal() { this.showModal = false; }
}
