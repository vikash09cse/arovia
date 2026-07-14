import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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

@Component({
  selector: 'app-lab-report',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './lab-report.component.html',
  styleUrl: './lab-report.component.scss'
})
export class LabReportComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly rows = signal<LabReportItem[]>([]);
  readonly totalVisits = signal(0);
  readonly loading = signal(true);
  readonly error = signal('');

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

    const query = new URLSearchParams();

    if (this.filterType === 'date') {
      if (this.dateFrom) query.set('dateFrom', this.dateFrom);
      if (this.dateTo) query.set('dateTo', this.dateTo);
    } else if (this.filterType === 'phone') {
      const digits = this.searchTerm.replace(/\D/g, '');
      if (digits.length < 10 || digits.length > 15) {
        this.error.set('Enter a valid phone number (10–15 digits).');
        this.rows.set([]);
        this.totalVisits.set(0);
        this.loading.set(false);
        return;
      }
      query.set('phone', digits);
    } else {
      const code = this.searchTerm.trim();
      if (!code) {
        this.error.set('Enter a patient number.');
        this.rows.set([]);
        this.totalVisits.set(0);
        this.loading.set(false);
        return;
      }
      query.set('patientCode', code);
    }

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
    this.loadReport();
  }

  clearFilters() {
    this.filterType = 'date';
    this.searchTerm = '';
    this.dateFrom = this.monthStartIso();
    this.dateTo = this.todayIso();
    this.loadReport();
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
