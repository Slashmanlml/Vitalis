import { Injectable } from '@angular/core';

export interface Toast { id: number; mensaje: string; tipo: 'success' | 'error' | 'info'; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts: Toast[] = [];
  private _id = 0;

  get toasts() { return this._toasts; }

  success(mensaje: string) { this.agregar(mensaje, 'success'); }
  error(mensaje: string) { this.agregar(mensaje, 'error'); }
  info(mensaje: string) { this.agregar(mensaje, 'info'); }

  private agregar(mensaje: string, tipo: Toast['tipo']) {
    const t: Toast = { id: ++this._id, mensaje, tipo };
    this._toasts.push(t);
    setTimeout(() => this.remover(t.id), 3500);
  }

  remover(id: number) { this._toasts = this._toasts.filter(t => t.id !== id); }
}
