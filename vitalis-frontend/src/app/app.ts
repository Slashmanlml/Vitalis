import { Component, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  // Angular 22 usa OnPush por defecto. Esta pantalla guarda lo que llega del
  // servidor en propiedades comunes (no signals), así que necesita la
  // detección de cambios "de siempre" para que se dibuje al llegar la respuesta.
  changeDetection: ChangeDetectionStrategy.Eager,
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('vitalis-frontend');
}
