import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { LayoutComponent } from './shared/layout/layout.component';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent), canActivate: [guestGuard] },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'tenants', loadComponent: () => import('./features/tenants/tenants.component').then(m => m.TenantsComponent) },
      { path: 'platform-users', loadComponent: () => import('./features/platform-users/platform-users.component').then(m => m.PlatformUsersComponent) },
      { path: 'portal-users', loadComponent: () => import('./features/portal-users/portal-users.component').then(m => m.PortalUsersComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
