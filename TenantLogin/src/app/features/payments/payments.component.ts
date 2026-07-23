import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';
import { downloadPaymentReceiptPdf, printPaymentReceipt } from '../../shared/receipt/receipt.util';

type PaymentsTab = 'all' | 'open' | 'pending';

interface PaymentListItem {
  id: string;
  visitId: string;
  visitCode: string;
  visitDateTime: string;
  visitStatusCode: number;
  patientCode: string;
  patientFirstName: string;
  patientLastName: string;
  patientFullName: string;
  amount: number;
  collectionDateTime?: string | null;
  collectedByName?: string | null;
  receiptNumber?: string | null;
  notes?: string | null;
  totalDue: number;
  totalCollected: number;
  balanceDue: number;
  createdAt: string;
}

interface PaymentList {
  items: PaymentListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface PendingVisitItem {
  visitId: string;
  visitCode: string;
  visitDateTime: string;
  visitStatusCode: number;
  patientId: string;
  patientCode: string;
  patientFirstName: string;
  patientLastName: string;
  patientFullName: string;
  doctorName?: string | null;
  totalDue: number;
  totalCollected: number;
  balanceDue: number;
}

interface PendingVisitList {
  items: PendingVisitItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './payments.component.html',
  styleUrl: './payments.component.scss'
})
export class PaymentsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  readonly payments = signal<PaymentListItem[]>([]);
  readonly pendingVisits = signal<PendingVisitItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly Math = Math;
  readonly receiptBusyId = signal<string | null>(null);

  searchTerm = '';
  dateFrom = '';
  dateTo = '';
  activeTab: PaymentsTab = 'all';

  ngOnInit() {
    const filter = this.route.snapshot.queryParamMap.get('filter');
    if (filter === 'open') {
      this.activeTab = 'open';
    } else if (filter === 'pending') {
      this.activeTab = 'pending';
    }

    this.load();
  }

  get isPendingTab(): boolean {
    return this.activeTab === 'pending';
  }

  load() {
    if (this.activeTab === 'pending') {
      this.loadPending();
    } else {
      this.loadPayments();
    }
  }

  loadPayments() {
    this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams({
      page: String(this.page()),
      pageSize: String(this.pageSize)
    });

    const term = this.searchTerm.trim();
    if (term) query.set('patientCode', term);
    if (this.dateFrom) query.set('dateFrom', this.dateFrom);
    if (this.dateTo) query.set('dateTo', this.dateTo);
    if (this.activeTab === 'open') query.set('openVisitsOnly', 'true');

    this.api.get<ApiResult<PaymentList>>(`/payments?${query}`).subscribe({
      next: res => {
        this.payments.set(res.data?.items ?? []);
        this.pendingVisits.set([]);
        this.totalCount.set(res.data?.totalCount ?? 0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load payments.');
        this.loading.set(false);
      }
    });
  }

  loadPending() {
    this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams({
      page: String(this.page()),
      pageSize: String(this.pageSize)
    });

    const term = this.searchTerm.trim();
    if (term) query.set('patientCode', term);
    if (this.dateFrom) query.set('dateFrom', this.dateFrom);
    if (this.dateTo) query.set('dateTo', this.dateTo);

    this.api.get<ApiResult<PendingVisitList>>(`/payments/pending?${query}`).subscribe({
      next: res => {
        this.pendingVisits.set(res.data?.items ?? []);
        this.payments.set([]);
        this.totalCount.set(res.data?.totalCount ?? 0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load pending payments.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.load();
  }

  setTab(tab: PaymentsTab) {
    this.activeTab = tab;
    this.page.set(1);
    this.load();
  }

  clearFilters() {
    this.searchTerm = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.activeTab = 'all';
    this.page.set(1);
    this.load();
  }

  prevPage() {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.load();
    }
  }

  nextPage() {
    const maxPage = Math.ceil(this.totalCount() / this.pageSize);
    if (this.page() < maxPage) {
      this.page.update(p => p + 1);
      this.load();
    }
  }

  formatMoney(value?: number | null): string {
    if (value == null) return '—';
    return `₹${value.toLocaleString('en-IN')}`;
  }

  formatDateTime(iso?: string | null): string {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
  }

  async printReceipt(paymentId: string) {
    this.receiptBusyId.set(paymentId);
    this.error.set('');
    try {
      await printPaymentReceipt(this.api, paymentId);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Unable to print receipt.';
      this.error.set(message);
    } finally {
      this.receiptBusyId.set(null);
    }
  }

  async downloadReceiptPdf(paymentId: string) {
    this.receiptBusyId.set(paymentId);
    this.error.set('');
    try {
      await downloadPaymentReceiptPdf(this.api, paymentId);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Unable to download PDF.';
      this.error.set(message);
    } finally {
      this.receiptBusyId.set(null);
    }
  }
}
