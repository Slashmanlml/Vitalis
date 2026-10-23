import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { evaluarPassword, generarPasswordSegura, RequisitoPassword } from '../utils/password';

/**
 * Medidor de fuerza + lista de requisitos + botón "Sugerir una contraseña".
 *
 * Los cuatro primeros requisitos son obligatorios (los exige el backend). El
 * símbolo es una RECOMENDACIÓN: sube el nivel del medidor pero no bloquea.
 */
@Component({
  selector: 'app-fuerza-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CommonModule],
  template: `
    <div class="fuerza" [attr.data-nivel]="evaluacion.nivel">
      <div class="medidor" aria-hidden="true">
        <span *ngFor="let i of [1,2,3,4]" [class.lleno]="password && evaluacion.nivel >= i"></span>
      </div>
      <p class="estado" aria-live="polite">
        <ng-container *ngIf="!password">Usá una frase o una combinación que no uses en otro lado.</ng-container>
        <ng-container *ngIf="password"><strong>{{ evaluacion.etiqueta }}.</strong> {{ evaluacion.consejo }}</ng-container>
      </p>

      <ul class="requisitos">
        <li *ngFor="let r of evaluacion.requisitos" [class.ok]="r.cumple" [class.opcional]="!r.obligatorio">
          <span class="marca" aria-hidden="true">
            <svg *ngIf="r.cumple" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>
          </span>
          <span>{{ r.texto }}<span class="sr">{{ r.cumple ? ' (cumple)' : ' (falta)' }}</span></span>
        </li>
      </ul>

      <button type="button" class="sugerir" (click)="sugerir()">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 2l-2 2m-7.6 7.6a5.5 5.5 0 1 1-7.8 7.8 5.5 5.5 0 0 1 7.8-7.8Zm0 0L15.5 7.5m0 0 3 3L22 7l-3-3m-3.5 3.5L19 4"/></svg>
        Sugerirme una contraseña segura
      </button>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .fuerza { display: flex; flex-direction: column; gap: 10px; margin-top: 10px; }
    .medidor { display: grid; grid-template-columns: repeat(4, 1fr); gap: 4px; }
    .medidor span { height: 4px; border-radius: 999px; background: var(--bg-secondary); transition: background-color .25s ease; }
    [data-nivel="1"] .medidor .lleno { background: var(--color-danger); }
    [data-nivel="2"] .medidor .lleno { background: var(--color-warning); }
    [data-nivel="3"] .medidor .lleno { background: var(--color-primary-light); }
    [data-nivel="4"] .medidor .lleno { background: var(--color-primary); }
    .estado { font-size: .8rem; color: var(--text-secondary); line-height: 1.45; }
    .estado strong { color: var(--text-primary); font-weight: 600; }
    .requisitos { list-style: none; display: grid; grid-template-columns: 1fr 1fr; gap: 5px 14px; }
    .requisitos li { display: flex; align-items: center; gap: 8px; font-size: .8rem; color: var(--text-muted); transition: color .2s ease; }
    .requisitos li.ok { color: var(--text-primary); }
    .marca { width: 16px; height: 16px; flex-shrink: 0; border-radius: 50%; display: grid; place-items: center; border: 1px solid var(--border-strong); transition: all .2s ease; }
    li.ok .marca { background: var(--color-primary); border-color: var(--color-primary); color: var(--text-on-color); }
    li.opcional:not(.ok) .marca { border-style: dashed; }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
    .sugerir { align-self: flex-start; display: inline-flex; align-items: center; gap: 7px; margin-top: 2px; padding: 6px 10px 6px 8px; border: none; border-radius: 8px; background: transparent; color: var(--color-primary); font-size: .8rem; font-weight: 550; cursor: pointer; }
    .sugerir:hover { background: var(--color-primary-bg); }
    .sugerir:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 1px; }
    @media (max-width: 420px) { .requisitos { grid-template-columns: 1fr; } }
  `]
})
export class FuerzaPasswordComponent {
  @Input() password = '';
  /** Emite la contraseña sugerida para que el formulario la use. */
  @Output() sugerida = new EventEmitter<string>();

  get evaluacion() {
    return evaluarPassword(this.password);
  }

  sugerir() {
    this.sugerida.emit(generarPasswordSegura());
  }
}

export type { RequisitoPassword };
