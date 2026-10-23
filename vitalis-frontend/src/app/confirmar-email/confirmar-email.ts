import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { mensajeDeError } from '../utils/password';
import { PanelMarcaComponent } from '../shared/panel-marca';

type Estado = 'confirmando' | 'ok' | 'error';

/**
 * Destino del enlace del correo: /confirmar-email?token=...
 *
 * El token viaja en la URL solo hasta esta página; la confirmación es un POST
 * al backend. Apenas se lee, se borra de la barra de direcciones para que no
 * quede en el historial del navegador.
 */
@Component({
  // Angular 22 usa OnPush por defecto. Esta pantalla guarda lo que llega del
  // servidor en propiedades comunes (no signals), así que necesita la
  // detección de cambios "de siempre" para que se dibuje al llegar la respuesta.
  changeDetection: ChangeDetectionStrategy.Eager,
  selector: 'app-confirmar-email',
  standalone: true,
  imports: [CommonModule, RouterLink, PanelMarcaComponent],
  templateUrl: './confirmar-email.html',
  styleUrls: ['../shared/auth.css', './confirmar-email.css']
})
export class ConfirmarEmailComponent implements OnInit {
  estado: Estado = 'confirmando';
  mensaje = '';

  constructor(private route: ActivatedRoute, private router: Router, private authService: AuthService) {}

  ngOnInit() {
    const token = this.route.snapshot.queryParamMap.get('token');
    this.router.navigate([], { replaceUrl: true, queryParams: {} });

    if (!token) {
      this.estado = 'error';
      this.mensaje = 'El enlace está incompleto. Abrilo de nuevo desde el correo.';
      return;
    }

    this.authService.confirmarEmail(token).subscribe({
      next: (r) => {
        this.estado = 'ok';
        this.mensaje = r.mensaje;
      },
      error: (err) => {
        this.estado = 'error';
        this.mensaje = mensajeDeError(err, 'No pudimos confirmar el correo.');
      }
    });
  }
}
