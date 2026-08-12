import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FacturaService } from '../services/factura.service';
import { PrestacionService } from '../services/prestacion.service';
import { PacienteService } from '../services/paciente.service';
import { CsvExportService } from '../services/csv-export.service';
import { Factura, CrearFactura, RegistrarPago } from '../models/factura.model';
import { Prestacion } from '../models/prestacion.model';
import { Paciente } from '../models/paciente.model';
import { PrintService } from '../services/print.service';

@Component({
  selector: 'app-facturacion',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './facturacion.html',
  styleUrls: ['./facturacion.css']
})
export class FacturacionComponent implements OnInit {
  facturas: Factura[] = [];
  pacientes: Paciente[] = [];
  prestaciones: Prestacion[] = [];
  filtroEstado: string = 'todas';
  showFacturaModal: boolean = false;
  showPagoModal: boolean = false;
  selectedFactura: Factura | null = null;

  facturaForm: any = { pacienteId: 0, observaciones: '', detalles: [] };
  pagoForm: RegistrarPago = { facturaId: 0, medioPago: 'Efectivo', importe: 0, observaciones: '' };
  nuevoDetalle: any = { prestacionId: 0, cantidad: 1, precioUnitario: 0 };

  constructor(
    private facturaService: FacturaService,
    private pacienteService: PacienteService,
    private prestacionService: PrestacionService,
    private printService: PrintService,
    private csvExportService: CsvExportService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargarFacturas();
    this.pacienteService.obtenerTodos().subscribe(d => {
      this.pacientes = d;
      this.cdr.detectChanges();
    });
    this.prestacionService.obtenerTodas().subscribe(d => {
      this.prestaciones = d;
      this.cdr.detectChanges();
    });
  }

  get filteredFacturas() {
    if (this.filtroEstado === 'todas') return this.facturas;
    return this.facturas.filter(f => f.estado === this.filtroEstado);
  }

  cargarFacturas() {
    this.facturaService.obtenerTodas().subscribe(d => {
      this.facturas = d;
      this.cdr.detectChanges();
    });
  }

  abrirNuevaFactura() {
    this.facturaForm = { pacienteId: 0, observaciones: '', detalles: [] };
    this.nuevoDetalle = { prestacionId: 0, cantidad: 1, precioUnitario: 0 };
    this.showFacturaModal = true;
  }

  agregarDetalle() {
    const prest = this.prestaciones.find(p => p.id === Number(this.nuevoDetalle.prestacionId));
    if (!prest) return;
    this.facturaForm.detalles.push({
      prestacionId: prest.id,
      prestacionNombre: prest.nombre,
      cantidad: this.nuevoDetalle.cantidad,
      precioUnitario: this.nuevoDetalle.precioUnitario || prest.importeBase
    });
    this.nuevoDetalle = { prestacionId: 0, cantidad: 1, precioUnitario: 0 };
  }

  quitarDetalle(i: number) { this.facturaForm.detalles.splice(i, 1); }

  get totalFactura() {
    return this.facturaForm.detalles.reduce((s: number, d: any) => s + (d.cantidad * d.precioUnitario), 0);
  }

  guardarFactura() {
    const dto: CrearFactura = {
      pacienteId: this.facturaForm.pacienteId,
      observaciones: this.facturaForm.observaciones,
      detalles: this.facturaForm.detalles.map((d: any) => ({
        prestacionId: d.prestacionId, cantidad: d.cantidad, precioUnitario: d.precioUnitario
      }))
    };
    this.facturaService.crear(dto).subscribe(() => {
      this.cargarFacturas();
      this.showFacturaModal = false;
      this.cdr.detectChanges();
    });
  }

  abrirPago(f: Factura) {
    this.selectedFactura = f;
    this.pagoForm = { facturaId: f.id, medioPago: 'Efectivo', importe: f.total - f.pagos.reduce((s, p) => s + p.importe, 0), observaciones: '' };
    this.showPagoModal = true;
  }

  guardarPago() {
    this.facturaService.registrarPago(this.pagoForm).subscribe(() => {
      this.cargarFacturas();
      this.showPagoModal = false;
      this.cdr.detectChanges();
    });
  }

  getEstadoClass(estado: string): string {
    const map: any = { 'Pagada': 'status-success', 'Pendiente': 'status-warning', 'Pago Parcial': 'status-info' };
    return map[estado] || 'status-pending';
  }

  getSaldo(f: Factura) {
    const pagado = f.pagos.reduce((s, p) => s + p.importe, 0);
    return f.total - pagado;
  }

  imprimirFactura(f: Factura) {
    this.printService.imprimir('factura-print-' + f.id, `Factura #${f.id} - ${f.pacienteNombre}`);
  }

  exportarFacturasCSV() {
    const data = this.filteredFacturas.map(f => ({
      'ID': f.id,
      'Paciente': f.pacienteNombre,
      'Fecha': new Date(f.fecha).toLocaleDateString('es-AR'),
      'Total': `$${f.total.toFixed(2)}`,
      'Pagado': `$${(f.pagos.reduce((s, p) => s + p.importe, 0)).toFixed(2)}`,
      'Saldo': `$${(f.total - f.pagos.reduce((s, p) => s + p.importe, 0)).toFixed(2)}`,
      'Estado': f.estado,
      'Observaciones': f.observaciones || '-'
    }));
    
    const timestamp = new Date().toISOString().slice(0, 10);
    this.csvExportService.exportToCSV(data, `facturas_${timestamp}`);
  }

  cerrarModal() { this.showFacturaModal = false; this.showPagoModal = false; }
}
