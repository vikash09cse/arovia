import { Component, computed, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface NavItem {
  label: string;
  route: string;
  icon: 'dashboard' | 'patients' | 'visits' | 'admissions' | 'payments' | 'lab' | 'doctors' | 'departments' | 'users' | 'settings' | 'addons' | 'templates' | 'files' | 'expenses' | 'reconcile' | 'diagnoses';
}

interface MyProfile {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  designation?: string | null;
}

const SIDEBAR_COLLAPSED_KEY = 'tenant_sidebar_collapsed';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent {
  private readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly mobileSidebarOpen = signal(false);
  readonly sidebarCollapsed = signal(this.loadCollapsedPreference());
  readonly user = computed(() => this.auth.currentUser());
  readonly hospitalName = computed(() => this.user()?.tenantName?.trim() || 'Hospital');
  readonly brandInitial = computed(() => {
    const name = this.hospitalName();
    return name.charAt(0).toUpperCase() || 'H';
  });
  readonly userMenuOpen = signal(false);

  readonly passwordModalOpen = signal(false);
  readonly profileModalOpen = signal(false);
  readonly accountSaving = signal(false);
  readonly accountError = signal('');
  readonly accountMessage = signal('');
  readonly profileLoading = signal(false);

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  profileEmail = '';
  profileFirstName = '';
  profileLastName = '';
  profileDesignation = '';

  readonly operationsNav: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Patients', route: '/patients', icon: 'patients' },
    { label: 'Visits', route: '/visits', icon: 'visits' },
    { label: 'Admissions', route: '/admissions', icon: 'admissions' },
    { label: 'Payments', route: '/payments', icon: 'payments' },
    { label: 'Lab Agencies', route: '/lab-tests', icon: 'lab' },
    { label: 'Lab Report', route: '/lab-report', icon: 'lab' },
    { label: 'Common Files', route: '/common-files', icon: 'files' }
  ];

  readonly adminNav = computed<NavItem[]>(() => {
    if (this.user()?.role !== 'TenantSuperAdmin') return [];

    return [
      { label: 'Departments', route: '/departments', icon: 'departments' },
      { label: 'Doctors', route: '/doctors', icon: 'doctors' },
      { label: 'Visit Add-ons', route: '/visit-addons', icon: 'addons' },
      { label: 'Users', route: '/users', icon: 'users' },
      { label: 'Diagnoses', route: '/diagnoses', icon: 'diagnoses' },
      { label: 'Expenses', route: '/expenses', icon: 'expenses' },
      { label: 'Monthly reconcile', route: '/monthly-reconcile', icon: 'reconcile' },
      { label: 'Templates', route: '/document-templates', icon: 'templates' },
      { label: 'Tenant Settings', route: '/settings', icon: 'settings' }
    ];
  });

  readonly showAdminSection = computed(() => this.adminNav().length > 0);

  constructor() {
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      takeUntilDestroyed()
    ).subscribe(() => {
      this.closeMobileSidebar();
      this.closeUserMenu();
    });
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.closeUserMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.passwordModalOpen() || this.profileModalOpen()) {
      this.closeAccountModals();
      return;
    }
    this.closeUserMenu();
  }

  menuAriaLabel(): string {
    if (this.isMobile()) {
      return this.mobileSidebarOpen() ? 'Close menu' : 'Open menu';
    }
    return this.sidebarCollapsed() ? 'Expand menu' : 'Collapse menu';
  }

  isMobile(): boolean {
    return typeof window !== 'undefined' && window.matchMedia('(max-width: 767px)').matches;
  }

  toggleMenu() {
    if (this.isMobile()) {
      this.mobileSidebarOpen.update(v => !v);
      return;
    }
    this.toggleCollapse();
  }

  toggleCollapse() {
    this.sidebarCollapsed.update(v => {
      const next = !v;
      sessionStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(next));
      return next;
    });
  }

  closeMobileSidebar() {
    this.mobileSidebarOpen.set(false);
  }

  onNavClick() {
    if (this.isMobile()) {
      this.closeMobileSidebar();
    }
  }

  toggleUserMenu(event: MouseEvent) {
    event.stopPropagation();
    this.userMenuOpen.update(v => !v);
  }

  closeUserMenu() {
    this.userMenuOpen.set(false);
  }

  openChangePassword(event: MouseEvent) {
    event.stopPropagation();
    this.closeUserMenu();
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.accountError.set('');
    this.accountMessage.set('');
    this.passwordModalOpen.set(true);
  }

  openUpdateProfile(event: MouseEvent) {
    event.stopPropagation();
    this.closeUserMenu();
    this.accountError.set('');
    this.accountMessage.set('');
    this.profileModalOpen.set(true);
    this.profileLoading.set(true);

    this.api.get<ApiResult<MyProfile>>('/auth/me').subscribe({
      next: res => {
        const p = res.data;
        this.profileEmail = p?.email ?? '';
        this.profileFirstName = p?.firstName ?? '';
        this.profileLastName = p?.lastName ?? '';
        this.profileDesignation = p?.designation ?? '';
        this.profileLoading.set(false);
      },
      error: err => {
        this.accountError.set(err.error?.message ?? 'Unable to load profile.');
        this.profileLoading.set(false);
      }
    });
  }

  closeAccountModals() {
    if (this.accountSaving()) return;
    this.passwordModalOpen.set(false);
    this.profileModalOpen.set(false);
    this.accountError.set('');
    this.accountMessage.set('');
  }

  submitChangePassword() {
    if (!this.currentPassword) {
      this.accountError.set('Current password is required.');
      return;
    }
    if (this.newPassword.trim().length < 6) {
      this.accountError.set('New password must be at least 6 characters.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.accountError.set('New password and confirmation do not match.');
      return;
    }

    this.accountSaving.set(true);
    this.accountError.set('');
    this.accountMessage.set('');

    this.api.post<ApiResult<boolean>>('/auth/change-password', {
      currentPassword: this.currentPassword,
      newPassword: this.newPassword.trim()
    }).subscribe({
      next: res => {
        this.accountSaving.set(false);
        this.accountMessage.set(res.message || 'Password updated successfully.');
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
      },
      error: err => {
        this.accountSaving.set(false);
        this.accountError.set(err.error?.message ?? 'Unable to update password.');
      }
    });
  }

  submitUpdateProfile() {
    if (!this.profileFirstName.trim() || !this.profileLastName.trim()) {
      this.accountError.set('First name and last name are required.');
      return;
    }

    this.accountSaving.set(true);
    this.accountError.set('');
    this.accountMessage.set('');

    this.api.put<ApiResult<MyProfile>>('/auth/me', {
      firstName: this.profileFirstName.trim(),
      lastName: this.profileLastName.trim(),
      designation: this.profileDesignation.trim() || null
    }).subscribe({
      next: res => {
        const p = res.data;
        if (p) {
          this.auth.updateLocalProfile(
            `${p.firstName} ${p.lastName}`.trim(),
            p.designation
          );
          this.profileFirstName = p.firstName;
          this.profileLastName = p.lastName;
          this.profileDesignation = p.designation ?? '';
        }
        this.accountSaving.set(false);
        this.accountMessage.set(res.message || 'Profile updated.');
      },
      error: err => {
        this.accountSaving.set(false);
        this.accountError.set(err.error?.message ?? 'Unable to update profile.');
      }
    });
  }

  logout() {
    this.closeUserMenu();
    this.auth.logout();
  }

  private loadCollapsedPreference(): boolean {
    if (typeof window === 'undefined') return false;
    const saved = sessionStorage.getItem(SIDEBAR_COLLAPSED_KEY);
    if (saved !== null) return saved === 'true';
    return window.matchMedia('(min-width: 768px) and (max-width: 1199px)').matches;
  }
}
