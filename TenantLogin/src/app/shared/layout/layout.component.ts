import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: 'dashboard' | 'patients' | 'visits' | 'payments' | 'lab' | 'doctors' | 'users' | 'settings' | 'addons';
}

const SIDEBAR_COLLAPSED_KEY = 'tenant_sidebar_collapsed';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly mobileSidebarOpen = signal(false);
  readonly sidebarCollapsed = signal(this.loadCollapsedPreference());
  readonly user = computed(() => this.auth.currentUser());

  readonly operationsNav: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Patients', route: '/patients', icon: 'patients' },
    { label: 'Visits', route: '/visits', icon: 'visits' },
    { label: 'Payments', route: '/payments', icon: 'payments' },
    { label: 'Lab Tests', route: '/lab-tests', icon: 'lab' }
  ];

  readonly adminNav = computed<NavItem[]>(() => {
    const items: NavItem[] = [];
    if (this.user()?.role === 'TenantSuperAdmin') {
      items.push(
        { label: 'Doctors', route: '/doctors', icon: 'doctors' },
        { label: 'Visit Add-ons', route: '/visit-addons', icon: 'addons' }
      );
    }
    items.push(
      { label: 'Users', route: '/users', icon: 'users' },
      { label: 'Tenant Settings', route: '/settings', icon: 'settings' }
    );
    return items;
  });

  constructor() {
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      takeUntilDestroyed()
    ).subscribe(() => this.closeMobileSidebar());
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

  logout() {
    this.auth.logout();
  }

  private loadCollapsedPreference(): boolean {
    if (typeof window === 'undefined') return false;
    const saved = sessionStorage.getItem(SIDEBAR_COLLAPSED_KEY);
    if (saved !== null) return saved === 'true';
    return window.matchMedia('(min-width: 768px) and (max-width: 1199px)').matches;
  }
}
