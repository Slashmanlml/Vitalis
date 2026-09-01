import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditoriaService, AuditoriaLog } from '../services/auditoria.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-auditorias',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auditorias.html',
  styleUrls: ['./auditorias.css']
})
export class AuditoriasComponent implements OnInit {
  logs: AuditoriaLog[] = [];
  filteredLogs: AuditoriaLog[] = [];
  
  searchTerm: string = '';
  selectedAccion: string = '';
  selectedTabla: string = '';
  
  uniqueTablas: string[] = [];

  showModal: boolean = false;
  selectedLog: AuditoriaLog | null = null;
  parsedValoresAnteriores: any = null;
  parsedValoresNuevos: any = null;

  // Las claves se calculan al abrir el detalle, no en la plantilla. Object.keys()
  // devuelve un array nuevo en cada llamada, y una función dentro de un *ngFor se
  // ejecuta en cada ciclo de detección de cambios: Angular reconstruía el bloque
  // completo una y otra vez. Es el mismo defecto que trababa la pantalla de
  // reportes.
  clavesAnteriores: string[] = [];
  clavesNuevas: string[] = [];

  constructor(
    private auditoriaService: AuditoriaService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.cargarLogs();
  }

  cargarLogs() {
    this.auditoriaService.obtenerTodas().subscribe({
      next: (data) => {
        this.logs = data;
        this.filteredLogs = data;
        
        const tablasSet = new Set(data.map(l => l.tabla));
        this.uniqueTablas = Array.from(tablasSet).sort();
        
        this.filtrar();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching audit logs', err);
        this.toastService.error('Error al cargar los registros de auditoría');
      }
    });
  }

  filtrar() {
    this.filteredLogs = this.logs.filter(log => {
      const matchSearch = !this.searchTerm || 
        (log.usuarioEmail && log.usuarioEmail.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (log.clavePrimaria && log.clavePrimaria.toLowerCase().includes(this.searchTerm.toLowerCase()));
        
      const matchAccion = !this.selectedAccion || log.accion === this.selectedAccion;
      const matchTabla = !this.selectedTabla || log.tabla === this.selectedTabla;
      
      return matchSearch && matchAccion && matchTabla;
    });
  }

  verDetalles(log: AuditoriaLog) {
    this.selectedLog = log;
    this.parsedValoresAnteriores = log.valoresAnteriores ? this.parseJson(log.valoresAnteriores) : null;
    this.parsedValoresNuevos = log.valoresNuevos ? this.parseJson(log.valoresNuevos) : null;
    this.clavesAnteriores = this.getObjectKeys(this.parsedValoresAnteriores);
    this.clavesNuevas = this.getObjectKeys(this.parsedValoresNuevos);
    this.showModal = true;
  }

  cerrarModal() {
    this.showModal = false;
    this.selectedLog = null;
    this.parsedValoresAnteriores = null;
    this.parsedValoresNuevos = null;
    this.clavesAnteriores = [];
    this.clavesNuevas = [];
  }

  private parseJson(jsonStr: string): any {
    try {
      return JSON.parse(jsonStr);
    } catch (e) {
      console.warn('Error parsing JSON from audit log', e);
      return jsonStr;
    }
  }

  getObjectKeys(obj: any): string[] {
    if (!obj || typeof obj !== 'object') return [];
    return Object.keys(obj);
  }
}
