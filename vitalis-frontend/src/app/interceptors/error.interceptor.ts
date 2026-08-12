import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../services/toast.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private toastService: ToastService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        let mensaje = 'Error de conexión con el servidor';

        if (error.error instanceof ErrorEvent) {
          mensaje = 'Error de red. Verifica tu conexión.';
        } else {
          if (typeof error.error === 'object' && error.error?.mensaje) {
            mensaje = error.error.mensaje;
          } else if (error.status === 0) {
            mensaje = 'No se puede conectar al servidor';
          } else if (error.status === 401) {
            mensaje = 'Sesión expirada. Inicia sesión nuevamente.';
            localStorage.removeItem('token');
            window.location.href = '/login';
          } else if (error.status === 403) {
            mensaje = 'No tienes permisos para realizar esta acción';
          } else if (error.status === 404) {
            mensaje = 'El recurso solicitado no fue encontrado';
          } else if (error.status === 500) {
            mensaje = 'Error interno del servidor';
          }
        }

        this.toastService.error(mensaje);
        console.error(`[HTTP Error ${error.status}]:`, mensaje, error);
        return throwError(() => error);
      })
    );
  }
}
