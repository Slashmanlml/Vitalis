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
        return this.escapeCSV(String(value || ''));
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
      cols.map(col => this.escapeCSV(String(item[col] || ''))).join(',')
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
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
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
