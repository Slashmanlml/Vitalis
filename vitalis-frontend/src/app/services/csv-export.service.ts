import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class CsvExportService {

  constructor() { }

  /**
   * Exports an array of objects to CSV format
   * @param data Array of objects to export
   * @param filename Name of the CSV file
   * @param columns Optional: array of column names to export (if not provided, all keys will be used)
   */
  exportToCSV<T extends object>(data: T[], filename: string, columns?: (keyof T)[]): void {
    if (!data || data.length === 0) {
      console.warn('No data to export');
      return;
    }

    // Determine which columns to export
    const cols = columns || (Object.keys(data[0]) as (keyof T)[]);
    
    // Create CSV header
    const header = cols.map(col => this.escapeCSV(String(col))).join(',');
    
    // Create CSV rows
    const rows = data.map(item =>
      cols.map(col => {
        const value = item[col];
        // No se usa (value || '') porque el número 0 es falsy: una fila con cero
        // turnos se exportaba como celda vacía en lugar de 0, y en un reporte
        // eso significa otra cosa (no hay dato vs. el dato es cero).
        return this.escapeCSV(value === null || value === undefined ? '' : String(value));
      }).join(',')
    );
    
    // Combine header and rows
    const csv = [header, ...rows].join('\n');
    
    // Create blob and download
    this.downloadCSV(csv, filename);
  }

  /**
   * Exports an array of objects with custom formatting
   * @param data Array of objects to export
   * @param filename Name of the CSV file
   * @param formatter Function to transform each row
   */
  exportToCSVCustom<T>(data: T[], filename: string, formatter: (item: T) => { [key: string]: any }): void {
    if (!data || data.length === 0) {
      console.warn('No data to export');
      return;
    }

    // Format all rows
    const formattedData = data.map(formatter);
    
    // Get columns from first formatted item
    const cols = Object.keys(formattedData[0]);
    
    // Create CSV header
    const header = cols.map(col => this.escapeCSV(col)).join(',');
    
    // Create CSV rows
    const rows = formattedData.map(item =>
      cols.map(col => {
        const valor = item[col];
        return this.escapeCSV(valor === null || valor === undefined ? '' : String(valor));
      }).join(',')
    );
    
    // Combine header and rows
    const csv = [header, ...rows].join('\n');
    
    // Create blob and download
    this.downloadCSV(csv, filename);
  }

  /**
   * Escapes special characters for CSV format
   */
  private escapeCSV(value: string): string {
    if (!value) return '""';
    if (value.includes(',') || value.includes('"') || value.includes('\n')) {
      return `"${value.replace(/"/g, '""')}"`;
    }
    return value;
  }

  /**
   * Triggers CSV file download
   */
  private downloadCSV(csv: string, filename: string): void {
    // Dos detalles que deciden si el archivo se abre bien o se abre roto en Excel:
    //
    // 1. El BOM (\uFEFF): sin él, Excel en Windows no reconoce el archivo como
    //    UTF-8 y muestra "MartÃ­nez" en lugar de "Martínez".
    // 2. La línea "sep=,": Excel usa el separador de listas de la configuración
    //    regional, que en Argentina es el punto y coma. Sin esta línea, todas
    //    las columnas caen dentro de la primera celda.
    const contenido = '\uFEFF' + 'sep=,\n' + csv;
    const blob = new Blob([contenido], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    link.setAttribute('href', url);
    link.setAttribute('download', `${filename}.csv`);
    link.style.visibility = 'hidden';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
