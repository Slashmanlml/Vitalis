import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection, LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeEsAr from '@angular/common/locales/es-AR';
import { provideRouter, Routes } from '@angular/router';
import { provideHttpClient, withInterceptors, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ErrorInterceptor } from './interceptors/error.interceptor';

import { LoginComponent } from './login/login';
import { authGuard, pacienteGuard } from './guards/auth.guard';


// Rutas perezosas (loadComponent): cada pantalla viaja en su propio archivo y se
// descarga recien cuando el usuario entra. Antes las 20 se importaban de entrada,
// de modo que el navegador bajaba Facturacion, Liquidaciones y Auditorias antes
// de poder mostrar el formulario de login: 880 kB para ver una pantalla que pesa
// una fraccion de eso.
//
// Login queda con importacion directa a proposito: es la primera pantalla, y
// cargarla aparte agregaria un viaje al servidor justo antes de lo unico que el
// usuario necesita ver al principio.
const routes: Routes = [
  { path: 'login', component: LoginComponent },
  // Páginas públicas del portal de pacientes.
  { path: 'registro', loadComponent: () => import('./registro/registro').then(m => m.RegistroComponent) },
  { path: 'confirmar-email', loadComponent: () => import('./confirmar-email/confirmar-email').then(m => m.ConfirmarEmailComponent) },
  {
    path: 'portal',
    loadComponent: () => import('./portal/portal').then(m => m.PortalComponent),
    canActivate: [pacienteGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard').then(m => m.DashboardComponent),
    canActivate: [authGuard],
    children: [
      { path: 'pacientes', loadComponent: () => import('./pacientes/pacientes').then(m => m.PacientesComponent) },
      // Ficha del paciente. Va DESPUÉS de la ruta del listado: el router de
      // Angular resuelve por orden y 'pacientes/:id' con un id vacío no debe
      // ganarle a 'pacientes'.
      { path: 'pacientes/:id', loadComponent: () => import('./pacientes/paciente-ficha').then(m => m.PacienteFichaComponent) },
      { path: 'profesionales', loadComponent: () => import('./profesionales/profesionales').then(m => m.ProfesionalesComponent) },
      { path: 'turnos', loadComponent: () => import('./turnos/turnos').then(m => m.TurnosComponent) },
      { path: 'obras-sociales', loadComponent: () => import('./obras-sociales/obras-sociales').then(m => m.ObrasSocialesComponent) },
      { path: 'especialidades', loadComponent: () => import('./especialidades/especialidades').then(m => m.EspecialidadesComponent) },
      { path: 'historia-clinica', loadComponent: () => import('./historia-clinica/historia-clinica').then(m => m.HistoriaClinicaComponent) },
      { path: 'prescripciones', loadComponent: () => import('./prescripciones/prescripciones').then(m => m.PrescripcionesComponent) },
      { path: 'sala-espera', loadComponent: () => import('./sala-espera/sala-espera').then(m => m.SalaEsperaComponent) },
      { path: 'medicamentos', loadComponent: () => import('./medicamentos/medicamentos').then(m => m.MedicamentosComponent) },
      { path: 'prestaciones', loadComponent: () => import('./prestaciones/prestaciones').then(m => m.PrestacionesComponent) },
      { path: 'facturacion', loadComponent: () => import('./facturacion/facturacion').then(m => m.FacturacionComponent) },
      { path: 'reportes', loadComponent: () => import('./reportes/reportes').then(m => m.ReportesComponent) },
      { path: 'liquidaciones', loadComponent: () => import('./liquidaciones/liquidaciones').then(m => m.LiquidacionesComponent) },
      { path: 'perfil', loadComponent: () => import('./perfil/perfil').then(m => m.PerfilComponent) },
      { path: 'auditorias', loadComponent: () => import('./auditorias/auditorias').then(m => m.AuditoriasComponent) },
      { path: 'bloqueos', loadComponent: () => import('./bloqueos/bloqueos').then(m => m.BloqueosComponent) },
      { path: 'usuarios', loadComponent: () => import('./usuarios/usuarios').then(m => m.UsuariosComponent) },
      { path: 'notificaciones', loadComponent: () => import('./email-logs/email-logs').then(m => m.EmailLogsComponent) },
      { path: 'mails-simulados', redirectTo: 'notificaciones', pathMatch: 'full' }
    ]
  },
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', redirectTo: '/login' }
];

// Fechas, días y meses en español de Argentina en todo el sistema.
registerLocaleData(localeEsAr);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    { provide: LOCALE_ID, useValue: 'es-AR' },
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    importProvidersFrom(FormsModule)
  ]
};
