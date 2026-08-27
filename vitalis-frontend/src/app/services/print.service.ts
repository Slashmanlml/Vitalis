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
          /* Esta ventana hereda las hojas de estilo de la aplicacion, incluida la
             regla @media (prefers-color-scheme: dark) de styles.css. Por eso todo
             lo de aca usa los tokens de PAPEL, que no tienen variante oscura: sin
             ellos, imprimir con el tema oscuro del sistema puesto produce una hoja
             negra con letras blancas. */
          html { background: var(--paper-bg); }
          body {
            padding: 40px;
            font-family: 'Segoe UI', system-ui, sans-serif;
            background: var(--paper-bg);
            color: var(--paper-text);
          }
          .no-print { display: none !important; }
          .print-header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid var(--paper-accent); padding-bottom: 15px; }
          .print-header h1 { margin: 0; font-size: 20px; color: var(--paper-text); }
          .print-header p { margin: 4px 0 0; color: var(--paper-text-muted); font-size: 13px; }
          table { width: 100%; border-collapse: collapse; margin: 16px 0; }
          th { background: var(--paper-bg-soft); padding: 10px 12px; text-align: left; font-size: 12px; font-weight: 600; color: var(--paper-text-muted); text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 2px solid var(--paper-border-light); }
          td { padding: 10px 12px; font-size: 13px; border-bottom: 1px solid var(--paper-border-light); }
          .print-footer { margin-top: 40px; text-align: center; color: var(--paper-text-muted); font-size: 11px; border-top: 1px solid var(--paper-border-light); padding-top: 15px; }
          @media print {
            body { padding: 0; }
            @page { margin: 20mm; }
            /* Sin esto los navegadores omiten fondos al imprimir y las celdas
               sombreadas de la tabla salen en blanco. */
            * { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
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
