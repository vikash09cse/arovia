import { Routes } from '@angular/router';
import { authGuard, guestGuard, tenantSuperAdminGuard } from './core/auth/auth.guard';
import { LoginComponent } from './features/login/login.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [guestGuard]
  },
  {
    path: '',
    loadComponent: () => import('./shared/layout/layout.component').then(m => m.LayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'patients',
        loadComponent: () => import('./features/patients/patients.component').then(m => m.PatientsComponent)
      },
      {
        path: 'patients/new',
        loadComponent: () => import('./features/patients/patient-form.component').then(m => m.PatientFormComponent)
      },
      {
        path: 'patients/:id',
        loadComponent: () => import('./features/patients/patient-detail.component').then(m => m.PatientDetailComponent)
      },
      {
        path: 'patients/:id/edit',
        loadComponent: () => import('./features/patients/patient-form.component').then(m => m.PatientFormComponent)
      },
      {
        path: 'visits',
        loadComponent: () => import('./features/visits/visits.component').then(m => m.VisitsComponent)
      },
      {
        path: 'visits/new',
        loadComponent: () => import('./features/visits/visit-form.component').then(m => m.VisitFormComponent)
      },
      {
        path: 'visits/:id',
        loadComponent: () => import('./features/visits/visit-detail.component').then(m => m.VisitDetailComponent)
      },
      {
        path: 'payments',
        loadComponent: () => import('./features/payments/payments.component').then(m => m.PaymentsComponent)
      },
      {
        path: 'lab-tests',
        loadComponent: () => import('./features/lab-tests/lab-agencies.component').then(m => m.LabAgenciesComponent)
      },
      {
        path: 'lab-report',
        loadComponent: () => import('./features/lab-tests/lab-report.component').then(m => m.LabReportComponent)
      },
      {
        path: 'visit-addons',
        canActivate: [tenantSuperAdminGuard],
        loadComponent: () => import('./features/visit-addons/visit-addons.component').then(m => m.VisitAddonsComponent)
      },
      {
        path: 'doctors',
        canActivate: [tenantSuperAdminGuard],
        loadComponent: () => import('./features/doctors/doctors.component').then(m => m.DoctorsComponent)
      },
      {
        path: 'doctors/new',
        canActivate: [tenantSuperAdminGuard],
        loadComponent: () => import('./features/doctors/doctor-form.component').then(m => m.DoctorFormComponent)
      },
      {
        path: 'doctors/:id/edit',
        canActivate: [tenantSuperAdminGuard],
        loadComponent: () => import('./features/doctors/doctor-form.component').then(m => m.DoctorFormComponent)
      },
      {
        path: 'users',
        canActivate: [tenantSuperAdminGuard],
        loadComponent: () => import('./features/users/users.component').then(m => m.UsersComponent)
      },
      {
        path: 'settings',
        canActivate: [tenantSuperAdminGuard],
        loadComponent: () => import('./shared/placeholder/placeholder.component').then(m => m.PlaceholderComponent),
        data: { title: 'Tenant Settings', description: 'Configure hospital profile, timezone, and preferences.' }
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
