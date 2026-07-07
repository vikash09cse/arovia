import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { RouterLink } from '@angular/router';

import { ApiService } from '../../core/api/api.service';

import { AuthService } from '../../core/auth/auth.service';

import { ApiResult } from '../../core/models/api.models';

import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';



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

  readonly deletingId = signal<string | null>(null);

  readonly confirmTarget = signal<VisitListItem | null>(null);

  readonly Math = Math;



  searchTerm = '';

  dateFrom = '';

  dateTo = '';

  visitTypeFilter = '';



  readonly confirmMessage = computed(() => {

    const visit = this.confirmTarget();

    if (!visit) return '';

    return `Visit ${visit.visitCode} for ${visit.patientFirstName} ${visit.patientLastName} will be cancelled. Any collected payments for this visit will remain on record.`;

  });



  ngOnInit() {

    const user = this.auth.currentUser();

    this.canEdit.set(user?.role === 'TenantSuperAdmin' || user?.role === 'Staff');

    this.loadVisits();

  }



  loadVisits() {

    this.loading.set(true);

    this.error.set('');



    const query = new URLSearchParams({

      page: String(this.page()),

      pageSize: String(this.pageSize)

    });



    const term = this.searchTerm.trim();

    if (term) {

      query.set('patientCode', term);

    }



    if (this.dateFrom) query.set('dateFrom', this.dateFrom);

    if (this.dateTo) query.set('dateTo', this.dateTo);

    if (this.visitTypeFilter) query.set('visitType', this.visitTypeFilter);



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

    this.searchTerm = '';

    this.dateFrom = '';

    this.dateTo = '';

    this.visitTypeFilter = '';

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



  cancelVisit(visit: VisitListItem) {

    if (visit.visitStatusCode !== 1) return;

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



    this.api.patch<ApiResult<boolean>>(`/visits/${visit.id}/cancel`, { reason: null }).subscribe({

      next: () => {

        this.deletingId.set(null);

        this.confirmTarget.set(null);

        this.loadVisits();

      },

      error: err => {

        this.error.set(err.error?.message ?? 'Unable to cancel visit.');

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

    return d.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });

  }



  paymentBadgeClass(code: number): string {

    switch (code) {

      case 2: return 'paid';

      case 1: return 'pending';

      case 3: return 'partial';

      default: return 'none';

    }

  }

}


