import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PrintService {
  imprimir(elementId: string, titulo: string) {
    const el = document.getElementById(elementId);
    if (!el) return;

    const win = window.open('', '_blank');
    if (!win) return;

    const estilos = Array.from(document.querySelectorAll('style, link[rel="stylesheet"]'))
      .map(s => s.outerHTML).join('\n');

    win.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>${titulo}</title>
        ${estilos}
        <style>
          body { padding: 40px; font-family: 'Segoe UI', system-ui, sans-serif; color: #1e293b; }
          .no-print { display: none !important; }
          .print-header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #4f46e5; padding-bottom: 15px; }
          .print-header h1 { margin: 0; font-size: 20px; color: #0f172a; }
          .print-header p { margin: 4px 0 0; color: #64748b; font-size: 13px; }
          table { width: 100%; border-collapse: collapse; margin: 16px 0; }
          th { background: #f8fafc; padding: 10px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 2px solid #e2e8f0; }
          td { padding: 10px 12px; font-size: 13px; border-bottom: 1px solid #f1f5f9; }
          .print-footer { margin-top: 40px; text-align: center; color: #94a3b8; font-size: 11px; border-top: 1px solid #e2e8f0; padding-top: 15px; }
          @media print {
            body { padding: 0; }
            @page { margin: 20mm; }
          }
        </style>
      </head>
      <body>
        <div class="print-header">
          <h1>${titulo}</h1>
          <p>Vitalis - Sistema de Gestión Médica</p>
        </div>
        ${el.innerHTML}
        <div class="print-footer">
          <p>Emitido el ${new Date().toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</p>
        </div>
        <script>window.print();window.onafterprint=()=>window.close();<\/script>
      </body>
      </html>
    `);
    win.document.close();
  }
}
