import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface TenantDashboard {
  totalPatientCount: number;
  todayNewPatientCount: number;
  todayVisitCount: number;
  todayRevenue: number;
  currentMonthRevenue: number;
  totalPendingAmount: number;
  todayLabAssignCount: number;
  currentMonthLabAssignCount: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);

  readonly user = computed(() => this.auth.currentUser());
  readonly isTenantAdmin = computed(() => this.auth.currentUser()?.role === 'TenantSuperAdmin');
  readonly metrics = signal<TenantDashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);
    this.error.set('');

    this.api.get<ApiResult<TenantDashboard>>('/dashboard').subscribe({
      next: res => {
        this.metrics.set(res.data ?? null);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load dashboard.');
        this.loading.set(false);
      }
    });
  }

  formatMoney(value: number | null | undefined): string {
    const amount = value ?? 0;
    return `₹${amount.toLocaleString('en-IN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
  }

  formatCount(value: number | null | undefined): string {
    return String(value ?? 0);
  }
}
