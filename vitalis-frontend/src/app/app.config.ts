import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import { provideHttpClient, withInterceptors, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ErrorInterceptor } from './interceptors/error.interceptor';

import { LoginComponent } from './login/login';
import { DashboardComponent } from './dashboard/dashboard';
import { PacientesComponent } from './pacientes/pacientes';
import { ProfesionalesComponent } from './profesionales/profesionales';
import { ObrasSocialesComponent } from './obras-sociales/obras-sociales';
import { EspecialidadesComponent } from './especialidades/especialidades';
import { TurnosComponent } from './turnos/turnos';
import { HistoriaClinicaComponent } from './historia-clinica/historia-clinica';
import { SalaEsperaComponent } from './sala-espera/sala-espera';
import { MedicamentosComponent } from './medicamentos/medicamentos';
import { PrestacionesComponent } from './prestaciones/prestaciones';
import { FacturacionComponent } from './facturacion/facturacion';
import { ReportesComponent } from './reportes/reportes';
import { LiquidacionesComponent } from './liquidaciones/liquidaciones';
import { PerfilComponent } from './perfil/perfil';
import { AuditoriasComponent } from './auditorias/auditorias';
import { BloqueosComponent } from './bloqueos/bloqueos';
import { EmailLogsComponent } from './email-logs/email-logs';
import { authGuard } from './guards/auth.guard';


const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    children: [
      { path: 'pacientes', component: PacientesComponent },
      { path: 'profesionales', component: ProfesionalesComponent },
      { path: 'turnos', component: TurnosComponent },
      { path: 'obras-sociales', component: ObrasSocialesComponent },
      { path: 'especialidades', component: EspecialidadesComponent },
      { path: 'historia-clinica', component: HistoriaClinicaComponent },
      { path: 'sala-espera', component: SalaEsperaComponent },
      { path: 'medicamentos', component: MedicamentosComponent },
      { path: 'prestaciones', component: PrestacionesComponent },
      { path: 'facturacion', component: FacturacionComponent },
      { path: 'reportes', component: ReportesComponent },
      { path: 'liquidaciones', component: LiquidacionesComponent },
      { path: 'perfil', component: PerfilComponent },
      { path: 'auditorias', component: AuditoriasComponent },
      { path: 'bloqueos', component: BloqueosComponent },
      { path: 'mails-simulados', component: EmailLogsComponent }
    ]
  },
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', redirectTo: '/login' }
];

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    importProvidersFrom(FormsModule)
  ]
};
