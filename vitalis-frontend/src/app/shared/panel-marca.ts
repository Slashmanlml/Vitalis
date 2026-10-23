import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Panel de marca de las pantallas públicas (login, registro, confirmar correo).
 *
 * Es el único lugar del sistema con un momento animado: un trazo de
 * electrocardiograma que se dibuja UNA vez al cargar y queda quieto. El resto
 * de la interfaz no tiene movimiento decorativo a propósito.
 */
@Component({
  selector: 'app-panel-marca',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CommonModule],
  template: `
    <aside class="panel" aria-hidden="false">
      <div class="marca">
        <span class="logo" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/>
            <path d="M3.22 12H9.5l1.5-3 2 6 1.5-3h4.78"/>
          </svg>
        </span>
        <span class="nombre">Vitalis</span>
      </div>

      <div class="centro">
        <h2 class="titulo">{{ titulo }}</h2>
        <p class="bajada">{{ bajada }}</p>
      </div>

      <svg class="trazo" viewBox="0 0 600 120" preserveAspectRatio="none" aria-hidden="true">
        <path class="linea-base" d="M0 70 H600"/>
        <path class="pulso" pathLength="1"
          d="M0 70 H150 L166 70 L174 58 L182 70 L196 70 L204 82 L214 18 L226 104 L236 70 L262 70 L276 54 L292 70 H420 L432 70 L438 62 L444 70 L454 70 L460 78 L468 36 L478 92 L486 70 L506 70 L518 60 L530 70 H600"/>
        <circle class="punto" cx="600" cy="70" r="4"/>
      </svg>

      <p class="pie">{{ pie }}</p>
    </aside>
  `,
  styles: [`
    :host { display: contents; }
    .panel {
      position: relative;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      gap: 40px;
      position: sticky;
      top: 0;
      align-self: start;
      height: 100vh;
      padding: 44px 48px 40px;
      background: var(--ink);
      color: var(--on-ink);
      isolation: isolate;
    }
    /* Anillos finos: la idea de un estudio por imágenes, apenas insinuada. */
    .panel::before {
      content: "";
      position: absolute;
      right: -28%;
      bottom: -38%;
      width: 120%;
      aspect-ratio: 1;
      border-radius: 50%;
      background:
        radial-gradient(circle, transparent 34%, var(--ink-line) 34.2%, transparent 34.6%),
        radial-gradient(circle, transparent 46%, var(--ink-line) 46.2%, transparent 46.6%),
        radial-gradient(circle, transparent 58%, var(--ink-line) 58.2%, transparent 58.6%),
        radial-gradient(circle, transparent 70%, var(--ink-line) 70.2%, transparent 70.6%),
        radial-gradient(circle at 50% 50%, color-mix(in srgb, var(--color-primary-light) 16%, transparent), transparent 62%);
      z-index: -1;
    }
    .marca { display: flex; align-items: center; gap: 11px; }
    .logo {
      width: 38px; height: 38px; border-radius: 11px;
      display: grid; place-items: center;
      background: var(--ink-raised);
      border: 1px solid var(--ink-line);
      color: var(--color-primary-light);
    }
    .logo svg { width: 20px; height: 20px; }
    .nombre { font-family: var(--font-display); font-size: 1.6rem; font-weight: 450; letter-spacing: -0.01em; }
    .centro { max-width: 30rem; }
    .titulo {
      font-family: var(--font-display);
      font-weight: 300;
      font-size: clamp(2.4rem, 3.4vw, 3.5rem);
      max-width: 10.5em;
      line-height: 1.04;
      letter-spacing: -0.03em;
      color: var(--on-ink);
      animation: entrar 1s var(--ease-out) both;
    }
    .bajada {
      margin-top: 18px;
      max-width: 38ch;
      font-size: 1rem;
      line-height: 1.55;
      color: var(--on-ink-muted);
      animation: entrar 1s .12s var(--ease-out) both;
    }
    .trazo { width: 100%; height: 110px; overflow: visible; }
    .linea-base { stroke: var(--ink-line); stroke-width: 1; fill: none; vector-effect: non-scaling-stroke; }
    .pulso {
      fill: none;
      stroke: var(--color-primary-light);
      stroke-width: 1.6;
      stroke-linejoin: round;
      stroke-linecap: round;
      vector-effect: non-scaling-stroke;
      stroke-dasharray: 1;
      stroke-dashoffset: 1;
      animation: dibujar 2.2s .35s cubic-bezier(.45,.05,.2,1) forwards;
    }
    .punto {
      fill: var(--color-primary-light);
      opacity: 0;
      animation: latido 1.1s 2.5s ease-out forwards;
    }
    .pie { font-size: .8rem; color: var(--on-ink-muted); max-width: 44ch; }
    @keyframes dibujar { to { stroke-dashoffset: 0; } }
    @keyframes latido {
      0% { opacity: 0; r: 2; }
      35% { opacity: 1; r: 6; }
      100% { opacity: 1; r: 4; }
    }
    @keyframes entrar { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: none; } }
    @media (prefers-reduced-motion: reduce) {
      .pulso { stroke-dashoffset: 0; animation: none; }
      .punto { opacity: 1; animation: none; }
    }
    @media (max-width: 900px) {
      .panel { position: relative; height: auto; padding: 22px 20px 18px; gap: 18px; }
      .centro, .pie { display: none; }
      .trazo { height: 46px; }
    }
  `]
})
export class PanelMarcaComponent {
  @Input() titulo = '';
  @Input() bajada = '';
  @Input() pie = 'Sistema de gestión para consultorio médico. Trabajo Final de Carrera, Análisis y Diseño de Sistemas.';
}
