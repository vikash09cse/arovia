import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';

type VisitFilterType = 'date' | 'phone' | 'patientCode' | 'visitCode';

interface VisitListItem {
  id: string;
  visitCode: string;
  visitDateTime: string;
  visitType: string;
  purpose: string;
  feeStatus: string;
  feeAmount?: number | null;
  procedureChargeAmount?: number | null;
  totalChargeAmount?: number | null;
  visitStatus: string;
  visitStatusCode: number;
  patientCode: string;
  patientFirstName: string;
  patientLastName: string;
  doctorFullName: string;
  aggregatedPaymentStatus: string;
  aggregatedPaymentStatusCode: number;
}

interface VisitList {
  items: VisitListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-visits',
  standalone: true,
  imports: [FormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './visits.component.html',
  styleUrl: './visits.component.scss'
})
export class VisitsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly visits = signal<VisitListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly canEdit = signal(false);
  readonly canDelete = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly confirmTarget = signal<VisitListItem | null>(null);
  readonly Math = Math;

  filterType: VisitFilterType = 'date';
  searchTerm = '';
  dateFrom = this.todayIso();
  dateTo = this.todayIso();

  readonly confirmMessage = computed(() => {
    const visit = this.confirmTarget();
    if (!visit) return '';
    return `Visit ${visit.visitCode} for ${visit.patientFirstName} ${visit.patientLastName} will be deleted and removed from the visits list and dashboard. Collected payments for this visit will remain on record.`;
  });

  filtersAreActive(): boolean {
    if (this.filterType !== 'date') return true;
    const today = this.todayIso();
    return this.dateFrom !== today || this.dateTo !== today;
  }

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canEdit.set(user?.role === 'TenantSuperAdmin' || user?.role === 'Staff');
    this.canDelete.set(user?.role === 'TenantSuperAdmin');
    this.loadVisits();
  }

  onFilterTypeChange() {
    this.searchTerm = '';
    this.dateFrom = this.todayIso();
    this.dateTo = this.todayIso();
    this.page.set(1);
    this.error.set('');

    if (this.filterType === 'date') {
      this.loadVisits();
    } else {
      this.visits.set([]);
      this.totalCount.set(0);
      this.loading.set(false);
    }
  }

  loadVisits() {
    this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams({
      page: String(this.page()),
      pageSize: String(this.pageSize)
    });

    if (this.filterType === 'date') {
      if (this.dateFrom) query.set('dateFrom', this.dateFrom);
      if (this.dateTo) query.set('dateTo', this.dateTo);
    } else if (this.filterType === 'phone') {
      const digits = this.searchTerm.replace(/\D/g, '');
      if (digits.length < 10 || digits.length > 15) {
        this.error.set('Enter a valid phone number (10–15 digits).');
        this.visits.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
        return;
      }
      query.set('phone', digits);
    } else if (this.filterType === 'patientCode') {
      const code = this.searchTerm.trim();
      if (!code) {
        this.error.set('Enter a patient number.');
        this.visits.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
        return;
      }
      query.set('patientCode', code);
    } else {
      const code = this.searchTerm.trim();
      if (!code) {
        this.error.set('Enter a visit ID.');
        this.visits.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
        return;
      }
      query.set('visitCode', code);
    }

    this.api.get<ApiResult<VisitList>>(`/visits?${query}`).subscribe({
      next: res => {
        this.visits.set(res.data?.items ?? []);
        this.totalCount.set(res.data?.totalCount ?? 0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load visits.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.loadVisits();
  }

  clearFilters() {
    this.filterType = 'date';
    this.searchTerm = '';
    this.dateFrom = this.todayIso();
    this.dateTo = this.todayIso();
    this.page.set(1);
    this.loadVisits();
  }

  prevPage() {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.loadVisits();
    }
  }

  nextPage() {
    const maxPage = Math.ceil(this.totalCount() / this.pageSize);
    if (this.page() < maxPage) {
      this.page.update(p => p + 1);
      this.loadVisits();
    }
  }

  deleteVisit(visit: VisitListItem) {
    this.confirmTarget.set(visit);
  }

  cancelDelete() {
    if (!this.deletingId()) {
      this.confirmTarget.set(null);
    }
  }

  confirmDelete() {
    const visit = this.confirmTarget();
    if (!visit) return;

    this.deletingId.set(visit.id);
    this.error.set('');

    this.api.delete<ApiResult<boolean>>(`/visits/${visit.id}`).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.confirmTarget.set(null);
        this.loadVisits();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to delete visit.');
        this.deletingId.set(null);
      }
    });
  }

  formatMoney(value?: number | null): string {
    if (value == null) return '—';
    return `₹${value.toLocaleString('en-IN')}`;
  }

  formatDateTime(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
      hour12: true
    });
  }

  paymentBadgeClass(code: number): string {
    switch (code) {
      case 2: return 'paid';
      case 1: return 'pending';
      case 3: return 'partial';
      default: return 'none';
    }
  }

  private todayIso(): string {
    return new Date().toLocaleDateString('en-CA');
  }
}
