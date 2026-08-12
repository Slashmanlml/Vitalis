import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TurnoService } from '../services/turno.service';
import { PacienteService } from '../services/paciente.service';
import { ProfesionalService } from '../services/profesional.service';
import { CsvExportService } from '../services/csv-export.service';
import { Turno } from '../models/turno.model';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.html',
  styleUrls: ['./reportes.css']
})
export class ReportesComponent implements OnInit {
  turnos: Turno[] = [];
  totalPacientes: number = 0;
  totalProfesionales: number = 0;
  periodo: string = '7';

  constructor(
    private turnoService: TurnoService,
    private pacienteService: PacienteService,
    private profesionalService: ProfesionalService,
    private csvExportService: CsvExportService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargarDatos();
  }

  cargarDatos() {
    this.pacienteService.obtenerTodos().subscribe(d => {
      this.totalPacientes = d.length;
      this.cdr.detectChanges();
    });
    this.profesionalService.obtenerTodos().subscribe(d => {
      this.totalProfesionales = d.length;
      this.cdr.detectChanges();
    });
    this.turnoService.obtenerTodos().subscribe(d => {
      this.turnos = d;
      this.cdr.detectChanges();
    });
  }

  get turnosDelPeriodo() {
    const dias = Number(this.periodo);
    const corte = new Date();
    corte.setDate(corte.getDate() - dias);
    return this.turnos.filter(t => new Date(t.fechaHora) >= corte);
  }

  get totalTurnos() { return this.turnosDelPeriodo.length; }
  get confirmados() { return this.turnosDelPeriodo.filter(t => t.confirmado).length; }
  get pendientes() { return this.turnosDelPeriodo.filter(t => !t.confirmado).length; }
  get ausentes() { return this.turnosDelPeriodo.filter(t => t.estado === 'Ausente').length; }
  get tasaAusentismo() {
    if (this.totalTurnos === 0) return 0;
    return Math.round((this.ausentes / this.totalTurnos) * 100);
  }

  get turnosPorDia(): any[] {
    const grupos: any = {};
    this.turnosDelPeriodo.forEach(t => {
      const dia = new Date(t.fechaHora).toLocaleDateString('es-AR');
      if (!grupos[dia]) grupos[dia] = { fecha: dia, cantidad: 0, confirmados: 0 };
      grupos[dia].cantidad++;
      if (t.confirmado) grupos[dia].confirmados++;
    });
    return Object.values(grupos).slice(0, 14);
  }

  get turnosPorProfesional(): any[] {
    const grupos: any = {};
    this.turnosDelPeriodo.forEach(t => {
      if (!grupos[t.profesionalNombre]) grupos[t.profesionalNombre] = { nombre: t.profesionalNombre, cantidad: 0 };
      grupos[t.profesionalNombre].cantidad++;
    });
    return Object.values(grupos).sort((a: any, b: any) => b.cantidad - a.cantidad);
  }

  exportarReporteTurnos() {
    const data = this.turnosDelPeriodo.map(t => ({
      'Fecha': new Date(t.fechaHora).toLocaleDateString('es-AR'),
      'Hora': new Date(t.fechaHora).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' }),
      'Paciente': t.pacienteNombre,
      'Médico': t.profesionalNombre,
      'Obra Social': t.obraSocialNombre,
      'Estado': t.confirmado ? 'Confirmado' : 'Pendiente'
    }));
    
    const timestamp = new Date().toISOString().slice(0, 10);
    this.csvExportService.exportToCSV(data, `reporte_turnos_${this.periodo}dias_${timestamp}`);
  }

  exportarResumenDiario() {
    const timestamp = new Date().toISOString().slice(0, 10);
    this.csvExportService.exportToCSV(this.turnosPorDia as any, `reporte_turnos_diarios_${timestamp}`);
  }

  exportarResumenProfesionales() {
    const timestamp = new Date().toISOString().slice(0, 10);
    this.csvExportService.exportToCSV(this.turnosPorProfesional as any, `reporte_turnos_profesionales_${timestamp}`);
  }
}
