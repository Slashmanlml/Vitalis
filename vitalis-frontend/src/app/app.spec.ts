import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

/**
 * Pruebas de humo del componente raíz.
 *
 * El archivo generado por Angular CLI verificaba que la página dijera
 * "Hello, vitalis-frontend" (el texto de la plantilla de andamio), cosa que
 * nunca fue cierta en este proyecto: la raíz sólo monta el router-outlet.
 * Estas pruebas comprueban lo que el componente hace de verdad.
 */
describe('App (componente raíz)', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('se crea sin errores', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('monta el router-outlet donde se renderizan las pantallas', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const dom = fixture.nativeElement as HTMLElement;
    expect(dom.querySelector('router-outlet')).not.toBeNull();
  });
});
