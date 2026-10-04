import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';

interface MonthlyTotals {
  opdRevenue: number;
  ipdRevenue: number;
  totalRevenue: number;
  totalExpenses: number;
  profit: number;
}

interface MonthlyProfile {
  id: string;
  yearMonth: string;
  status: string;
  statusCode: number;
  opdRevenue: number;
  ipdRevenue: number;
  totalRevenue: number;
  totalExpenses: number;
  profit: number;
  note?: string | null;
  reconciledAt?: string | null;
  reconciledBy?: string | null;
  reconcilerName?: string | null;
  createdAt: string;
}

interface MonthlyReconcile {
  yearMonth: string;
  isReconciled: boolean;
  totals: MonthlyTotals;
  isLive: boolean;
  profile?: MonthlyProfile | null;
}

function currentYearMonth(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

function formatYearMonth(yearMonth: string): string {
  const [y, m] = yearMonth.split('-').map(Number);
  return new Date(y, m - 1, 1).toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
}

@Component({
  selector: 'app-monthly-reconcile',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './monthly-reconcile.component.html',
  styleUrl: './monthly-reconcile.component.scss'
})
export class MonthlyReconcileComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly loading = signal(true);
  readonly acting = signal(false);
  readonly error = signal('');
  readonly msg = signal('');
  readonly yearMonth = signal(currentYearMonth());
  readonly isReconciled = signal(false);
  readonly isLive = signal(true);
  readonly totals = signal<MonthlyTotals>({
    opdRevenue: 0,
    ipdRevenue: 0,
    totalRevenue: 0,
    totalExpenses: 0,
    profit: 0
  });
  readonly profile = signal<MonthlyProfile | null>(null);
  readonly history = signal<MonthlyProfile[]>([]);

  note = '';

  readonly monthLabel = computed(() => formatYearMonth(this.yearMonth()));

  ngOnInit() {
    this.loadAll();
  }

  onMonthChange(value: string) {
    if (!value) return;
    this.yearMonth.set(value);
    this.error.set('');
    this.msg.set('');
    this.loadMonth();
  }

  selectHistoryMonth(ym: string) {
    this.onMonthChange(ym);
  }

  loadAll() {
    this.loadMonth();
    this.loadHistory();
  }

  loadMonth() {
    this.loading.set(true);
    this.error.set('');
    this.api.get<ApiResult<MonthlyReconcile>>(`/monthly-reconcile?yearMonth=${this.yearMonth()}`).subscribe({
      next: res => {
        const data = res.data;
        this.isReconciled.set(!!data?.isReconciled);
        this.isLive.set(data?.isLive ?? true);
        this.totals.set(data?.totals ?? {
          opdRevenue: 0,
          ipdRevenue: 0,
          totalRevenue: 0,
          totalExpenses: 0,
          profit: 0
        });
        this.profile.set(data?.profile ?? null);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load monthly reconcile.');
        this.loading.set(false);
      }
    });
  }

  loadHistory() {
    this.api.get<ApiResult<MonthlyProfile[]>>('/monthly-reconcile/history').subscribe({
      next: res => {
        this.history.set(res.data ?? []);
      }
    });
  }

  reconcile() {
    this.acting.set(true);
    this.error.set('');
    this.msg.set('');
    this.api.post<ApiResult<MonthlyProfile>>('/monthly-reconcile/reconcile', {
      yearMonth: this.yearMonth(),
      note: this.note.trim() || null
    }).subscribe({
      next: () => {
        this.acting.set(false);
        this.note = '';
        this.msg.set(`Reconciled ${this.monthLabel()}. Totals are locked.`);
        this.loadAll();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to reconcile month.');
        this.acting.set(false);
      }
    });
  }

  reopen() {
    this.acting.set(true);
    this.error.set('');
    this.msg.set('');
    this.api.post<ApiResult<boolean>>('/monthly-reconcile/reopen', {
      yearMonth: this.yearMonth()
    }).subscribe({
      next: () => {
        this.acting.set(false);
        this.msg.set(`Reopened ${this.monthLabel()}. Totals will follow live collections again.`);
        this.loadAll();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to reopen month.');
        this.acting.set(false);
      }
    });
  }

  formatMoney(value: number | null | undefined): string {
    const amount = value ?? 0;
    return `₹${amount.toLocaleString('en-IN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
  }

  formatDateTime(value?: string | null): string {
    if (!value) return '—';
    const d = new Date(value);
    return d.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
  }

  formatYearMonthLabel(ym: string): string {
    return formatYearMonth(ym);
  }
}
