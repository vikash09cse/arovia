import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';
import { downloadPaymentReceiptPdf, printPaymentReceipt } from '../../shared/receipt/receipt.util';

interface LookupItem {
  id: string;
  fullName: string;
}

interface PaymentCollection {
  id: string;
  amount: number;
  receiptNumber?: string | null;
  collectionDateTime?: string | null;
  collectedByUserId?: string | null;
  collectedByName?: string | null;
  notes?: string | null;
  paymentMethod?: string | null;
  paymentMethodCode?: number | null;
}

interface LabAgencyLookup {
  id: string;
  name: string;
  contactPerson?: string | null;
  phone?: string | null;
}

interface VisitLabAgency {
  id: string;
  labAgencyId: string;
  agencyName: string;
  assignedAt: string;
  assignedByUserId: string;
  assignedByName?: string | null;
  testName?: string | null;
  notes?: string | null;
}

interface VisitAddonLine {
  id: string;
  visitAddonId: string;
  addonName: string;
  amount: number;
  createdAt: string;
}

interface VisitDetail {
  id: string;
  visitCode: string;
  visitDateTime: string;
  visitType: string;
  purpose: string;
  visitNotes?: string | null;
  scheduledSurgeryDate?: string | null;
  feeStatus: string;
  feeAmount?: number | null;
  procedureChargeAmount?: number | null;
  totalChargeAmount?: number | null;
  discountAmount?: number | null;
  discountReason?: string | null;
  grossSubtotal: number;
  visitStatus: string;
  visitStatusCode: number;
  patientId: string;
  patientCode: string;
  patientFirstName: string;
  patientLastName: string;
  doctorFullName: string;
  totalDue: number;
  totalCollected: number;
  balanceDue: number;
  aggregatedPaymentStatus: string;
  aggregatedPaymentStatusCode: number;
  paymentCollections: PaymentCollection[];
  labAgencies: VisitLabAgency[];
  addons: VisitAddonLine[];
  canEditFees?: boolean;
}

@Component({
  selector: 'app-visit-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './visit-detail.component.html',
  styleUrl: './visit-detail.component.scss'
})
export class VisitDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly visit = signal<VisitDetail | null>(null);
  readonly collectors = signal<LookupItem[]>([]);
  readonly activeLabAgencies = signal<LabAgencyLookup[]>([]);
  readonly canEdit = signal(false);
  readonly canManageLab = signal(false);
  readonly savingNotes = signal(false);
  readonly addingPayment = signal(false);
  readonly voidingPaymentId = signal<string | null>(null);
  readonly receiptBusyId = signal<string | null>(null);
  readonly showAddPayment = signal(false);
  readonly assigningLab = signal(false);
  readonly removingLabId = signal<string | null>(null);
  readonly showAssignLab = signal(false);
  readonly savingDiscount = signal(false);
  readonly showDiscountForm = signal(false);

  visitNotes = '';
  discountEditAmount: number | null = null;
  discountEditReason = '';
  paymentAmount: number | null = null;
  paymentCollectedByUserId = '';
  paymentNotes = '';
  paymentMethod = 1;
  selectedLabAgencyId = '';
  labAssignmentTestName = '';
  labAssignmentNotes = '';

  readonly canManagePayments = computed(() => {
    const v = this.visit();
    return this.canEdit() && v?.visitStatusCode === 1;
  });

  readonly canManageDiscount = computed(() => {
    const v = this.visit();
    if (!this.canEdit() || v?.visitStatusCode !== 1) return false;

    const role = this.auth.currentUser()?.role;
    if (role === 'TenantSuperAdmin') return true;
    if (role === 'Staff') return this.isVisitToday(v.visitDateTime);
    return false;
  });

  readonly isStaffUser = computed(() => this.auth.currentUser()?.role === 'Staff');

  readonly hasBalanceDue = computed(() => {
    const v = this.visit();
    return (v?.balanceDue ?? 0) > 0;
  });

  readonly availableLabAgencies = computed(() => {
    const assigned = new Set((this.visit()?.labAgencies ?? []).map(a => a.labAgencyId));
    return this.activeLabAgencies().filter(a => !assigned.has(a.id));
  });

  ngOnInit() {
    const user = this.auth.currentUser();
    const role = user?.role;
    this.canEdit.set(role === 'TenantSuperAdmin' || role === 'Staff');
    this.canManageLab.set(role === 'TenantSuperAdmin' || role === 'Staff' || role === 'Doctor');
    if (user?.userId) {
      this.paymentCollectedByUserId = user.userId;
    }

    this.loadCollectors();
    this.loadActiveLabAgencies();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadVisit(id);
  }

  loadActiveLabAgencies() {
    this.api.get<ApiResult<LabAgencyLookup[]>>('/lab-agencies/active').subscribe({
      next: res => this.activeLabAgencies.set(res.data ?? []),
      error: () => this.error.set('Unable to load lab agencies.')
    });
  }

  openAssignLab() {
    this.selectedLabAgencyId = '';
    this.labAssignmentTestName = '';
    this.labAssignmentNotes = '';
    this.showAssignLab.set(true);
    this.error.set('');
  }

  cancelAssignLab() {
    this.showAssignLab.set(false);
    this.labAssignmentTestName = '';
    this.labAssignmentNotes = '';
  }

  submitLabAssignment() {
    const v = this.visit();
    if (!v) return;

    if (!this.selectedLabAgencyId) {
      this.error.set('Please select a lab agency.');
      return;
    }

    if (this.labAssignmentTestName.length > 250) {
      this.error.set('Test name cannot exceed 250 characters.');
      return;
    }

    this.assigningLab.set(true);
    this.error.set('');

    this.api.post<ApiResult<unknown>>(`/visits/${v.id}/lab-agencies`, {
      labAgencyId: this.selectedLabAgencyId,
      testName: this.labAssignmentTestName.trim() || null,
      notes: this.labAssignmentNotes.trim() || null
    }).subscribe({
      next: () => {
        this.assigningLab.set(false);
        this.showAssignLab.set(false);
        this.labAssignmentTestName = '';
        this.labAssignmentNotes = '';
        this.loadVisit(v.id);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to assign lab agency.');
        this.assigningLab.set(false);
      }
    });
  }

  removeLabAssignment(assignment: VisitLabAgency) {
    const v = this.visit();
    if (!v) return;

    if (!confirm(`Remove ${assignment.agencyName} from this visit?`)) return;

    this.removingLabId.set(assignment.id);
    this.error.set('');

    this.api.delete<ApiResult<unknown>>(`/visits/${v.id}/lab-agencies/${assignment.id}`).subscribe({
      next: () => {
        this.removingLabId.set(null);
        this.loadVisit(v.id);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to remove lab agency assignment.');
        this.removingLabId.set(null);
      }
    });
  }

  loadCollectors() {
    this.api.get<ApiResult<LookupItem[]>>('/visits/payment-collectors').subscribe({
      next: res => {
        const list = res.data ?? [];
        this.collectors.set(list);
        if (!this.paymentCollectedByUserId && list.length === 1) {
          this.paymentCollectedByUserId = list[0].id;
        }
      },
      error: () => this.error.set('Unable to load staff for payment collection.')
    });
  }

  loadVisit(id: string) {
    this.loading.set(true);

    this.api.get<ApiResult<VisitDetail>>(`/visits/${id}`).subscribe({
      next: res => {
        const data = res.data ?? null;
        if (data && !data.labAgencies) {
          data.labAgencies = [];
        }
        if (data && !data.addons) {
          data.addons = [];
        }
        this.visit.set(data);
        this.visitNotes = data?.visitNotes ?? '';
        this.syncDiscountFormFromVisit(data);
        if (!this.canManageDiscount()) {
          this.showDiscountForm.set(false);
        }
        if (data && this.paymentAmount == null) {
          this.paymentAmount = data.balanceDue > 0 ? data.balanceDue : null;
        }
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load visit.');
        this.loading.set(false);
      }
    });
  }

  openAddPayment(fullBalance = false) {
    const v = this.visit();
    if (!v) return;
    this.paymentAmount = fullBalance ? v.balanceDue : v.balanceDue;
    this.paymentNotes = '';
    this.paymentMethod = 1;
    this.showAddPayment.set(true);
    this.error.set('');
  }

  cancelAddPayment() {
    this.showAddPayment.set(false);
    this.paymentNotes = '';
  }

  syncDiscountFormFromVisit(v: VisitDetail | null) {
    if (!v) return;
    this.discountEditAmount = v.discountAmount ?? null;
    this.discountEditReason = v.discountReason ?? '';
  }

  openDiscountForm() {
    const v = this.visit();
    if (!v) return;
    this.syncDiscountFormFromVisit(v);
    this.showDiscountForm.set(true);
    this.error.set('');
  }

  cancelDiscountForm() {
    this.showDiscountForm.set(false);
    this.syncDiscountFormFromVisit(this.visit());
  }

  submitDiscount() {
    const v = this.visit();
    if (!v) return;

    if (!this.canManageDiscount()) {
      this.error.set('Fee and price changes are only allowed on the day of the visit.');
      return;
    }

    const amount = this.discountEditAmount ?? 0;
    if (amount < 0) {
      this.error.set('Invalid discount amount.');
      return;
    }
    if (amount > v.grossSubtotal) {
      this.error.set('Discount cannot exceed the visit subtotal.');
      return;
    }
    if (amount > 0 && !this.discountEditReason.trim()) {
      this.error.set('Discount reason is required when applying a discount.');
      return;
    }

    this.savingDiscount.set(true);
    this.error.set('');

    this.api.patch<ApiResult<VisitDetail>>(`/visits/${v.id}/discount`, {
      discountAmount: amount,
      discountReason: amount > 0 ? this.discountEditReason.trim() : null
    }).subscribe({
      next: () => {
        this.savingDiscount.set(false);
        this.showDiscountForm.set(false);
        this.loadVisit(v.id);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to apply discount.');
        this.savingDiscount.set(false);
      }
    });
  }

  clearDiscount() {
    this.discountEditAmount = 0;
    this.discountEditReason = '';
    this.submitDiscount();
  }

  submitPayment() {
    const v = this.visit();
    if (!v) return;

    const amount = this.paymentAmount ?? 0;
    if (amount <= 0) {
      this.error.set('Amount must be greater than zero.');
      return;
    }
    if (amount > v.balanceDue) {
      this.error.set(`Amount cannot exceed remaining balance (${this.formatMoney(v.balanceDue)}).`);
      return;
    }
    if (!this.paymentCollectedByUserId) {
      this.error.set('Please select who collected the payment.');
      return;
    }

    this.addingPayment.set(true);
    this.error.set('');

    const body: Record<string, unknown> = {
      amount,
      collectedByUserId: this.paymentCollectedByUserId,
      notes: this.paymentNotes.trim() || null,
      paymentMethod: this.paymentMethod
    };

    this.api.post<ApiResult<unknown>>(`/visits/${v.id}/payments`, body).subscribe({
      next: () => {
        this.addingPayment.set(false);
        this.showAddPayment.set(false);
        this.paymentNotes = '';
        this.loadVisit(v.id);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to record payment.');
        this.addingPayment.set(false);
      }
    });
  }

  voidCollection(collection: PaymentCollection) {
    const v = this.visit();
    if (!v) return;

    if (!confirm(`Void receipt ${collection.receiptNumber ?? collection.id}? This restores the amount to the visit balance.`)) {
      return;
    }

    this.voidingPaymentId.set(collection.id);
    this.error.set('');

    this.api.patch<ApiResult<unknown>>(`/payments/${collection.id}/void`, {}).subscribe({
      next: () => {
        this.voidingPaymentId.set(null);
        this.loadVisit(v.id);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to void payment.');
        this.voidingPaymentId.set(null);
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

  saveNotes() {
    const v = this.visit();
    if (!v) return;

    this.savingNotes.set(true);

    this.api.patch<ApiResult<VisitDetail>>(`/visits/${v.id}/notes`, { visitNotes: this.visitNotes }).subscribe({
      next: res => {
        this.visit.set(res.data ?? this.visit());
        this.savingNotes.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to update notes.');
        this.savingNotes.set(false);
      }
    });
  }

  formatMoney(value?: number | null): string {
    if (value == null) return '—';
    return `₹${value.toLocaleString('en-IN')}`;
  }

  /** Staff fee edits are allowed only on the visit's calendar day (India time). */
  isVisitToday(visitDateTime: string): boolean {
    const tz = 'Asia/Kolkata';
    const visitDay = new Date(visitDateTime).toLocaleDateString('en-CA', { timeZone: tz });
    const today = new Date().toLocaleDateString('en-CA', { timeZone: tz });
    return visitDay === today;
  }

  paymentBadgeClass(code: number): string {
    switch (code) {
      case 2: return 'paid';
      case 3: return 'partial';
      case 1: return 'pending';
      default: return 'none';
    }
  }
}
