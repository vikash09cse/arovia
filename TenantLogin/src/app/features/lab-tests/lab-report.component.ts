import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';

type LabReportFilterType = 'date' | 'phone' | 'patientCode';

interface LabReportItem {
  id: string;
  name: string;
  contactPerson?: string | null;
  phone?: string | null;
  status: string;
  statusCode: number;
  visitCount: number;
}

interface LabReportResponse {
  items: LabReportItem[];
  totalVisitAssignments: number;
}

interface LabReportDetailItem {
  id: string;
  visitId: string;
  patientId: string;
  patientCode: string;
  patientName: string;
  visitDateTime: string;
  assignedAt: string;
  testName?: string | null;
  notes?: string | null;
}

interface LabReportDetailResponse {
  labAgencyId: string;
  labAgencyName: string;
  items: LabReportDetailItem[];
}

@Component({
  selector: 'app-lab-report',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './lab-report.component.html',
  styleUrl: './lab-report.component.scss'
})
export class LabReportComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly rows = signal<LabReportItem[]>([]);
  readonly totalVisits = signal(0);
  readonly loading = signal(true);
  readonly error = signal('');

  readonly drawerOpen = signal(false);
  readonly drawerAgencyName = signal('');
  readonly detailRows = signal<LabReportDetailItem[]>([]);
  readonly detailLoading = signal(false);
  readonly detailError = signal('');

  filterType: LabReportFilterType = 'date';
  searchTerm = '';
  dateFrom = this.monthStartIso();
  dateTo = this.todayIso();

  ngOnInit() {
    this.loadReport();
  }

  filtersAreActive(): boolean {
    if (this.filterType !== 'date') return true;
    return this.dateFrom !== this.monthStartIso() || this.dateTo !== this.todayIso();
  }

  onFilterTypeChange() {
    this.searchTerm = '';
    this.dateFrom = this.monthStartIso();
    this.dateTo = this.todayIso();
    this.error.set('');
    this.closeDrawer();

    if (this.filterType === 'date') {
      this.loadReport();
    } else {
      this.rows.set([]);
      this.totalVisits.set(0);
      this.loading.set(false);
    }
  }

  loadReport() {
    this.loading.set(true);
    this.error.set('');

    const query = this.buildFilterQuery();
    if (!query) return;

    this.api.get<ApiResult<LabReportResponse>>(`/lab-agencies/assignment-report?${query}`).subscribe({
      next: res => {
        this.rows.set(res.data?.items ?? []);
        this.totalVisits.set(res.data?.totalVisitAssignments ?? 0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load lab report.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.closeDrawer();
    this.loadReport();
  }

  clearFilters() {
    this.filterType = 'date';
    this.searchTerm = '';
    this.dateFrom = this.monthStartIso();
    this.dateTo = this.todayIso();
    this.closeDrawer();
    this.loadReport();
  }

  openDetails(row: LabReportItem) {
    if (row.visitCount <= 0) return;

    const query = this.buildFilterQuery();
    if (!query) return;

    this.drawerAgencyName.set(row.name);
    this.drawerOpen.set(true);
    this.detailLoading.set(true);
    this.detailError.set('');
    this.detailRows.set([]);

    this.api
      .get<ApiResult<LabReportDetailResponse>>(`/lab-agencies/${row.id}/assignment-report-details?${query}`)
      .subscribe({
        next: res => {
          this.drawerAgencyName.set(res.data?.labAgencyName || row.name);
          this.detailRows.set(res.data?.items ?? []);
          this.detailLoading.set(false);
        },
        error: err => {
          this.detailError.set(err.error?.message ?? 'Unable to load assignments.');
          this.detailLoading.set(false);
        }
      });
  }

  closeDrawer() {
    this.drawerOpen.set(false);
    this.detailError.set('');
    this.detailRows.set([]);
  }

  formatDateTime(value: string | null | undefined): string {
    if (!value) return '—';
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return '—';
    return d.toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private buildFilterQuery(): URLSearchParams | null {
    const query = new URLSearchParams();

    if (this.filterType === 'date') {
      if (this.dateFrom) query.set('dateFrom', this.dateFrom);
      if (this.dateTo) query.set('dateTo', this.dateTo);
      return query;
    }

    if (this.filterType === 'phone') {
      const digits = this.searchTerm.replace(/\D/g, '');
      if (digits.length < 10 || digits.length > 15) {
        this.error.set('Enter a valid phone number (10–15 digits).');
        this.rows.set([]);
        this.totalVisits.set(0);
        this.loading.set(false);
        return null;
      }
      query.set('phone', digits);
      return query;
    }

    const code = this.searchTerm.trim();
    if (!code) {
      this.error.set('Enter a patient number.');
      this.rows.set([]);
      this.totalVisits.set(0);
      this.loading.set(false);
      return null;
    }
    query.set('patientCode', code);
    return query;
  }

  private todayIso(): string {
    return new Date().toLocaleDateString('en-CA');
  }

  private monthStartIso(): string {
    const d = new Date();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    return `${yyyy}-${mm}-01`;
  }
}
