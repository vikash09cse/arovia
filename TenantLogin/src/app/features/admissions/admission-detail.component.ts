import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';
import { downloadPaymentReceiptPdf, printPaymentReceipt } from '../../shared/receipt/receipt.util';

interface AdmissionCharge {
  id: string;
  chargeCategoryCode: number;
  chargeCategory: string;
  description: string;
  amount: number;
  chargedOn: string;
  createdByName?: string | null;
}

interface AdmissionPayment {
  id: string;
  amount: number;
  paymentMethodCode: number;
  paymentMethod: string;
  paymentKindCode: number;
  paymentKind: string;
  receiptNumber?: string | null;
  notes?: string | null;
  collectionDateTime: string;
  collectedByUserId: string;
  collectedByName?: string | null;
}

interface AdmissionDetail {
  id: string;
  admissionCode: string;
  admittedAt: string;
  dischargedAt?: string | null;
  ward: string;
  bed?: string | null;
  roomClass: string;
  status: string;
  statusCode: number;
  estimatedAmount: number;
  notes?: string | null;
  discountAmount: number;
  discountReason?: string | null;
  fromVisitId?: string | null;
  fromVisitCode?: string | null;
  departmentId: string;
  departmentName: string;
  patientId: string;
  patientCode: string;
  patientFullName: string;
  attendingDoctorId: string;
  doctorName: string;
  chargesTotal: number;
  billTotal: number;
  paidTotal: number;
  balanceDue: number;
  charges: AdmissionCharge[];
  payments: AdmissionPayment[];
}

interface LookupItem {
  id: string;
  fullName: string;
}

@Component({
  selector: 'app-admission-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe],
  templateUrl: './admission-detail.component.html',
  styleUrl: './admission-detail.component.scss'
})
export class AdmissionDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly admission = signal<AdmissionDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly message = signal('');
  readonly canManage = signal(false);
  readonly collectors = signal<LookupItem[]>([]);
  readonly busy = signal(false);
  readonly receiptBusyId = signal<string | null>(null);

  showCharge = false;
  showPay = false;
  showDiscount = false;

  chargeCategory = 1;
  chargeDescription = '';
  chargeAmount: number | null = null;

  discountAmount: number | null = null;
  discountReason = '';

  payAmount: number | null = null;
  payMethod = 1;
  payKind = 2;
  payCollectorId = '';
  payNotes = '';

  readonly chargeCategories = [
    { code: 1, label: 'Room' },
    { code: 2, label: 'Procedure' },
    { code: 3, label: 'Lab' },
    { code: 4, label: 'Pharmacy' },
    { code: 5, label: 'Doctor' },
    { code: 6, label: 'Other' }
  ];

  ngOnInit() {
    const role = this.auth.currentUser()?.role;
    this.canManage.set(role === 'TenantSuperAdmin' || role === 'Staff');
    const userId = this.auth.currentUser()?.userId;
    if (userId) this.payCollectorId = userId;

    this.loadCollectors();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.load(id);
  }

  loadCollectors() {
    this.api.get<ApiResult<LookupItem[]>>('/visits/payment-collectors').subscribe({
      next: res => this.collectors.set(res.data ?? [])
    });
  }

  load(id: string) {
    this.loading.set(true);
    this.error.set('');
    this.api.get<ApiResult<AdmissionDetail>>(`/admissions/${id}`).subscribe({
      next: res => {
        this.admission.set(res.data ?? null);
        if (res.data) {
          this.discountAmount = res.data.discountAmount;
          this.discountReason = res.data.discountReason ?? '';
        }
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load admission.');
        this.loading.set(false);
      }
    });
  }

  openCharge() {
    this.showCharge = true;
    this.chargeCategory = 1;
    this.chargeDescription = '';
    this.chargeAmount = null;
  }

  openPay() {
    this.showPay = true;
    const bal = this.admission()?.balanceDue ?? 0;
    this.payAmount = bal > 0 ? bal : null;
    this.payKind = bal > 0 ? 3 : 2;
    this.payNotes = '';
  }

  openDiscount() {
    this.showDiscount = true;
    const a = this.admission();
    this.discountAmount = a?.discountAmount ?? 0;
    this.discountReason = a?.discountReason ?? '';
  }

  collectFullBalance() {
    const bal = this.admission()?.balanceDue ?? 0;
    if (bal > 0) {
      this.payAmount = bal;
      this.payKind = 3;
    }
  }

  addCharge() {
    const a = this.admission();
    if (!a) return;
    if (!this.chargeDescription.trim()) {
      this.error.set('Charge description is required.');
      return;
    }
    if (this.chargeAmount == null || this.chargeAmount < 0) {
      this.error.set('Valid charge amount is required.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.api.post<ApiResult<AdmissionDetail>>(`/admissions/${a.id}/charges`, {
      chargeCategory: this.chargeCategory,
      description: this.chargeDescription.trim(),
      amount: this.chargeAmount
    }).subscribe({
      next: res => {
        this.admission.set(res.data ?? null);
        this.showCharge = false;
        this.message.set('Charge added.');
        this.busy.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to add charge.');
        this.busy.set(false);
      }
    });
  }

  applyDiscount() {
    const a = this.admission();
    if (!a) return;
    const amount = this.discountAmount ?? 0;
    if (amount > 0 && !this.discountReason.trim()) {
      this.error.set('Discount reason is required.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.api.patch<ApiResult<AdmissionDetail>>(`/admissions/${a.id}/discount`, {
      discountAmount: amount,
      discountReason: amount > 0 ? this.discountReason.trim() : null
    }).subscribe({
      next: res => {
        this.admission.set(res.data ?? null);
        this.showDiscount = false;
        this.message.set('Discount updated.');
        this.busy.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to apply discount.');
        this.busy.set(false);
      }
    });
  }

  addPayment() {
    const a = this.admission();
    if (!a) return;
    if (this.payAmount == null || this.payAmount <= 0) {
      this.error.set('Payment amount must be greater than zero.');
      return;
    }
    if (!this.payCollectorId) {
      this.error.set('Select a collector.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.api.post<ApiResult<AdmissionDetail>>(`/admissions/${a.id}/payments`, {
      amount: this.payAmount,
      collectedByUserId: this.payCollectorId,
      paymentMethod: this.payMethod,
      paymentKind: this.payKind,
      notes: this.payNotes.trim() || null
    }).subscribe({
      next: res => {
        this.admission.set(res.data ?? null);
        this.showPay = false;
        this.message.set('Payment recorded.');
        this.busy.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to record payment.');
        this.busy.set(false);
      }
    });
  }

  discharge() {
    const a = this.admission();
    if (!a) return;
    if (a.balanceDue > 0) {
      this.error.set(`Cannot discharge while balance remains (${a.balanceDue}). Collect final payment first.`);
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.api.post<ApiResult<AdmissionDetail>>(`/admissions/${a.id}/discharge`, {}).subscribe({
      next: res => {
        this.admission.set(res.data ?? null);
        this.message.set('Patient discharged.');
        this.busy.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to discharge.');
        this.busy.set(false);
      }
    });
  }

  async printReceipt(paymentId: string) {
    this.receiptBusyId.set(paymentId);
    this.error.set('');
    try {
      await printPaymentReceipt(this.api, paymentId);
    } catch (err: unknown) {
      this.error.set(err instanceof Error ? err.message : 'Unable to print receipt.');
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
      this.error.set(err instanceof Error ? err.message : 'Unable to download PDF.');
    } finally {
      this.receiptBusyId.set(null);
    }
  }
}
