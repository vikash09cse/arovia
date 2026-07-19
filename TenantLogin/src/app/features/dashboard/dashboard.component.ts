import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  todayPendingAmount: number;
  todayLabAssignCount: number;
  currentMonthLabAssignCount: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, RouterLink],
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
  readonly visibleRevenueCards = signal<ReadonlySet<string>>(new Set());

  dateFrom = this.todayIso();
  dateTo = this.todayIso();

  ngOnInit() {
    this.loadDashboard();
  }

  isRevenueVisible(cardKey: string): boolean {
    return this.visibleRevenueCards().has(cardKey);
  }

  toggleRevenueVisibility(cardKey: string, event?: Event) {
    event?.preventDefault();
    event?.stopPropagation();
    this.visibleRevenueCards.update(current => {
      const next = new Set(current);
      if (next.has(cardKey)) next.delete(cardKey);
      else next.add(cardKey);
      return next;
    });
  }

  displayMoney(cardKey: string, value: number | null | undefined): string {
    if (!this.isRevenueVisible(cardKey)) return '₹XX';
    return this.formatMoney(value);
  }

  filtersAreActive(): boolean {
    const today = this.todayIso();
    return this.dateFrom !== today || this.dateTo !== today;
  }

  loadDashboard() {
    const initialLoad = this.metrics() == null;
    if (initialLoad) this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams();
    if (this.dateFrom) query.set('dateFrom', this.dateFrom);
    if (this.dateTo) query.set('dateTo', this.dateTo);

    this.api.get<ApiResult<TenantDashboard>>(`/dashboard?${query}`).subscribe({
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

  onSearch() {
    this.loadDashboard();
  }

  clearFilters() {
    this.dateFrom = this.todayIso();
    this.dateTo = this.todayIso();
    this.loadDashboard();
  }

  formatMoney(value: number | null | undefined): string {
    const amount = value ?? 0;
    return `₹${amount.toLocaleString('en-IN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
  }

  formatCount(value: number | null | undefined): string {
    return String(value ?? 0);
  }

  private todayIso(): string {
    return new Date().toLocaleDateString('en-CA');
  }
}
