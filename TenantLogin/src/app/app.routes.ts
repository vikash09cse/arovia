import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent),
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
        path: 'visit-addons',
        loadComponent: () => import('./features/visit-addons/visit-addons.component').then(m => m.VisitAddonsComponent)
      },
      {
        path: 'doctors',
        loadComponent: () => import('./features/doctors/doctors.component').then(m => m.DoctorsComponent)
      },
      {
        path: 'doctors/new',
        loadComponent: () => import('./features/doctors/doctor-form.component').then(m => m.DoctorFormComponent)
      },
      {
        path: 'doctors/:id/edit',
        loadComponent: () => import('./features/doctors/doctor-form.component').then(m => m.DoctorFormComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./features/users/users.component').then(m => m.UsersComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./shared/placeholder/placeholder.component').then(m => m.PlaceholderComponent),
        data: { title: 'Tenant Settings', description: 'Configure hospital profile, timezone, and preferences.' }
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
