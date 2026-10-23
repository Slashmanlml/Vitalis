import { ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Campo de contraseña con botón para mostrarla u ocultarla.
 *
 * Se usa con [(valor)]: <app-campo-password [(valor)]="datos.password" ...>.
 * El botón es un <button type="button"> de verdad (se alcanza con Tab y lo lee
 * un lector de pantalla), y su etiqueta cambia según el estado.
 */
@Component({
  selector: 'app-campo-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CommonModule],
  template: `
    <div class="campo" [class.invalido]="invalido" [class.deshabilitado]="disabled">
      <input
        #entrada
        [id]="inputId"
        [attr.name]="name"
        [type]="visible ? 'text' : 'password'"
        [value]="valor"
        (input)="cambiar($event)"
        (blur)="salir.emit()"
        [attr.autocomplete]="autocomplete"
        [attr.placeholder]="placeholder || null"
        [attr.aria-invalid]="invalido"
        [attr.aria-describedby]="describedBy || null"
        [disabled]="disabled"
        spellcheck="false"
        autocapitalize="off" />
      <button
        type="button"
        class="ojo"
        (click)="alternar()"
        [disabled]="disabled"
        [attr.aria-label]="visible ? 'Ocultar contraseña' : 'Mostrar contraseña'"
        [attr.aria-pressed]="visible"
        [title]="visible ? 'Ocultar contraseña' : 'Mostrar contraseña'">
        <svg *ngIf="!visible" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/>
        </svg>
        <svg *ngIf="visible" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M9.9 4.24A9.1 9.1 0 0 1 12 4c6.4 0 10 8 10 8a17 17 0 0 1-2.16 3.19"/><path d="M6.6 6.6A17.2 17.2 0 0 0 2 12s3.6 8 10 8a9.7 9.7 0 0 0 5.4-1.6"/><path d="M14.1 14.1a3 3 0 1 1-4.2-4.2"/><line x1="2" y1="2" x2="22" y2="22"/>
        </svg>
      </button>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .campo {
      position: relative;
      display: flex;
      align-items: center;
      border: 1px solid var(--border-color);
      border-radius: 10px;
      background: var(--bg-card);
      transition: border-color .15s ease, box-shadow .15s ease, background-color .15s ease;
    }
    .campo:hover:not(.deshabilitado):not(:focus-within) { border-color: var(--border-strong); }
    .campo:focus-within { border-color: var(--color-primary); box-shadow: var(--focus-ring); }
    .campo.invalido { border-color: var(--color-danger); background: var(--danger-bg); }
    .campo.invalido:focus-within { box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-danger) 20%, transparent); }
    .campo.deshabilitado { background: var(--bg-secondary); }
    input {
      flex: 1;
      min-width: 0;
      height: 46px;
      padding: 0 4px 0 14px;
      border: none;
      outline: none;
      background: transparent;
      color: var(--text-primary);
      font-size: 0.95rem;
      letter-spacing: 0.01em;
    }
    input[type="password"] { letter-spacing: 0.12em; }
    .ojo {
      width: 40px;
      height: 40px;
      margin-right: 3px;
      display: grid;
      place-items: center;
      border: none;
      border-radius: 8px;
      background: transparent;
      color: var(--text-muted);
      cursor: pointer;
      transition: color .15s ease, background-color .15s ease;
    }
    .ojo:hover:not(:disabled) { color: var(--text-primary); background: var(--bg-secondary); }
    .ojo:focus-visible { outline: 2px solid var(--color-primary); outline-offset: -2px; }
  `]
})
export class CampoPasswordComponent {
  @Input() valor = '';
  @Output() valorChange = new EventEmitter<string>();
  @Output() salir = new EventEmitter<void>();

  @Input() inputId = '';
  @Input() name = '';
  @Input() autocomplete = 'current-password';
  @Input() placeholder = '';
  @Input() invalido = false;
  @Input() disabled = false;
  @Input() describedBy = '';

  /** Se puede forzar desde afuera (al sugerir una contraseña, se muestra). */
  @Input() visible = false;

  @ViewChild('entrada') private entrada?: ElementRef<HTMLInputElement>;

  cambiar(e: Event) {
    this.valor = (e.target as HTMLInputElement).value;
    this.valorChange.emit(this.valor);
  }

  alternar() {
    this.visible = !this.visible;
    // Al cambiar el tipo, el cursor vuelve al final del texto.
    queueMicrotask(() => {
      const el = this.entrada?.nativeElement;
      if (el && document.activeElement !== el) return;
      el?.setSelectionRange(el.value.length, el.value.length);
    });
  }

  enfocar() {
    this.entrada?.nativeElement.focus();
  }
}
