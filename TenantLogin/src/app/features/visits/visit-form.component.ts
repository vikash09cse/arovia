import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface PatientListItem {
  id: string;
  patientCode: string;
  firstName: string;
  lastName: string;
}

interface PatientList {
  items: PatientListItem[];
}

interface LookupItem {
  id: string;
  fullName: string;
}

interface FeePreview {
  proposedFeeStatus: string;
  proposedFeeStatusCode: number;
  proposedFeeAmount?: number | null;
  tenantVisitFeeAmount: number;
  freeVisitWindowDays: number;
  lastChargedVisitDateTime?: string | null;
  daysSinceLastCharged?: number | null;
}

interface VisitAddonLookup {
  id: string;
  name: string;
  code?: string | null;
  defaultAmount: number;
}

@Component({
  selector: 'app-visit-form',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './visit-form.component.html',
  styleUrl: './visit-form.component.scss'
})
export class VisitFormComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly defaultConsultationFee = 500;

  readonly saving = signal(false);
  readonly error = signal('');
  readonly doctors = signal<LookupItem[]>([]);
  readonly collectors = signal<LookupItem[]>([]);
  readonly feePreview = signal<FeePreview | null>(null);
  readonly previewLoading = signal(false);
  readonly isSuperAdmin = signal(false);
  readonly activeAddons = signal<VisitAddonLookup[]>([]);

  patientSearch = '';
  readonly selectedPatient = signal<PatientListItem | null>(null);

  visitType = 1;
  consultingDoctorId = '';
  purpose = '';
  visitNotes = '';
  procedureChargeAmount: number | null = null;
  scheduledSurgeryDate = '';
  consultationFeeStatus = 1;
  consultationFeeAmount: number | null = 500;
  feeNote = '';
  collectAtCreate = false;
  initialCollectionAmount: number | null = null;
  collectedByUserId = '';
  paymentMethod = 1;
  readonly selectedAddonIds = signal<Set<string>>(new Set());
  discountAmount: number | null = null;
  discountReason = '';

  get hasCharges(): boolean {
    const consultation = this.consultationFeeStatus === 1 && (this.consultationFeeAmount ?? 0) > 0;
    const procedure = (this.procedureChargeAmount ?? 0) > 0;
    const addons = this.selectedAddonIds().size > 0;
    return consultation || procedure || addons;
  }

  get showPaymentFields(): boolean {
    const consultationCharged = this.consultationFeeStatus === 1;
    const procedure = (this.procedureChargeAmount ?? 0) > 0;
    const addons = this.selectedAddonIds().size > 0;
    return consultationCharged || procedure || addons;
  }

  get consultationDue(): number {
    if (this.consultationFeeStatus !== 1) return 0;
    return this.consultationFeeAmount ?? 0;
  }

  get addonsDue(): number {
    const selected = this.selectedAddonIds();
    return this.activeAddons()
      .filter(a => selected.has(a.id))
      .reduce((sum, a) => sum + a.defaultAmount, 0);
  }

  get grossSubtotal(): number {
    const proc = this.procedureChargeAmount ?? 0;
    return this.consultationDue + (proc > 0 ? proc : 0) + this.addonsDue;
  }

  get discountDue(): number {
    const amount = this.discountAmount ?? 0;
    return amount > 0 ? amount : 0;
  }

  get totalDue(): number {
    const net = this.grossSubtotal - this.discountDue;
    return net > 0 ? net : 0;
  }

  ngOnInit() {
    const user = this.auth.currentUser();
    this.isSuperAdmin.set(user?.role === 'TenantSuperAdmin');
    if (user?.userId) {
      this.collectedByUserId = user.userId;
    }
    this.loadDoctors();
    this.loadCollectors();
    this.loadActiveAddons();

    const patientId = this.route.snapshot.queryParamMap.get('patientId');
    const type = this.route.snapshot.queryParamMap.get('visitType');
    if (type) this.visitType = +type;

    if (patientId) {
      this.loadPatientById(patientId);
    }
  }

  loadDoctors() {
    this.api.get<ApiResult<LookupItem[]>>('/doctors/active').subscribe({
      next: res => {
        const list = res.data ?? [];
        this.doctors.set(list);
        if (list.length === 1) {
          this.consultingDoctorId = list[0].id;
        }
      },
      error: () => this.error.set('Unable to load doctors.')
    });
  }

  loadCollectors() {
    this.api.get<ApiResult<LookupItem[]>>('/visits/payment-collectors').subscribe({
      next: res => {
        const list = res.data ?? [];
        this.collectors.set(list);
        if (!this.collectedByUserId && list.length === 1) {
          this.collectedByUserId = list[0].id;
        }
      },
      error: () => this.error.set('Unable to load staff for payment collection.')
    });
  }

  loadActiveAddons() {
    this.api.get<ApiResult<VisitAddonLookup[]>>('/visit-addons/active').subscribe({
      next: res => this.activeAddons.set(res.data ?? []),
      error: () => this.error.set('Unable to load visit add-ons.')
    });
  }

  isAddonSelected(id: string): boolean {
    return this.selectedAddonIds().has(id);
  }

  toggleAddon(id: string) {
    this.selectedAddonIds.update(current => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
    this.syncCollectionAmount();
  }

  selectedAddonItems(): VisitAddonLookup[] {
    const selected = this.selectedAddonIds();
    return this.activeAddons().filter(a => selected.has(a.id));
  }

  loadPatientById(id: string) {
    this.api.get<ApiResult<{ id: string; patientCode: string; firstName: string; lastName: string }>>(`/patients/${id}`).subscribe({
      next: res => {
        if (res.data) {
          this.selectedPatient.set(res.data);
          this.patientSearch = `${res.data.patientCode} — ${res.data.firstName} ${res.data.lastName}`;
          this.loadFeePreview(res.data.id);
        }
      }
    });
  }

  searchPatients() {
    const term = this.patientSearch.trim();
    if (!term) return;

    const query = new URLSearchParams({ page: '1', pageSize: '10' });
    const digits = term.replace(/\D/g, '');
    if (digits.length >= 10) {
      query.set('phone', digits);
    } else {
      query.set('patientCode', term);
    }

    this.api.get<ApiResult<PatientList>>(`/patients?${query}`).subscribe({
      next: res => {
        const items = res.data?.items ?? [];
        if (items.length === 1) {
          this.selectPatient(items[0]);
        } else if (items.length === 0) {
          this.error.set('No patient found.');
          this.resetPatient();
        } else {
          this.error.set('Multiple patients found — refine your search.');
        }
      },
      error: err => this.error.set(err.error?.message ?? 'Patient search failed.')
    });
  }

  selectPatient(p: PatientListItem) {
    this.selectedPatient.set(p);
    this.patientSearch = `${p.patientCode} — ${p.firstName} ${p.lastName}`;
    this.error.set('');
    this.loadFeePreview(p.id);
  }

  resetPatient() {
    this.selectedPatient.set(null);
    this.feePreview.set(null);
    this.patientSearch = '';
    this.consultationFeeStatus = 1;
    this.consultationFeeAmount = this.defaultConsultationFee;
    this.feeNote = '';
    this.collectAtCreate = false;
    this.initialCollectionAmount = null;
    this.selectedAddonIds.set(new Set());
    this.discountAmount = null;
    this.discountReason = '';
  }

  loadFeePreview(patientId: string) {
    this.previewLoading.set(true);
    this.api.get<ApiResult<FeePreview>>(`/visits/fee-preview?patientId=${patientId}`).subscribe({
      next: res => {
        const preview = res.data ?? null;
        this.feePreview.set(preview);
        if (preview) {
          this.applyFeeDefaults(preview);
        }
        this.previewLoading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load fee preview.');
        this.previewLoading.set(false);
      }
    });
  }

  applyFeeDefaults(preview: FeePreview) {
    this.consultationFeeStatus = 1;
    this.consultationFeeAmount = preview.tenantVisitFeeAmount > 0
      ? preview.tenantVisitFeeAmount
      : this.defaultConsultationFee;
  }

  onConsultationFeeStatusChange() {
    if (this.consultationFeeStatus === 2) {
      this.consultationFeeAmount = null;
    } else if (this.consultationFeeAmount == null) {
      const preview = this.feePreview();
      const tenantFee = preview?.tenantVisitFeeAmount;
      this.consultationFeeAmount = tenantFee != null && tenantFee > 0
        ? tenantFee
        : this.defaultConsultationFee;
    }

    this.syncCollectionAmount();
  }

  onProcedureChargeChange() {
    this.syncCollectionAmount();
  }

  onDiscountChange() {
    this.syncCollectionAmount();
  }

  private syncCollectionAmount() {
    if (this.collectAtCreate) {
      this.initialCollectionAmount = this.totalDue > 0 ? this.totalDue : null;
    }
  }

  onCollectAtCreateChange() {
    if (this.collectAtCreate) {
      this.initialCollectionAmount = this.totalDue > 0 ? this.totalDue : null;
    } else {
      this.initialCollectionAmount = null;
    }
  }

  formatMoney(value: number): string {
    return `₹${value.toLocaleString('en-IN')}`;
  }

  submit() {
    const patient = this.selectedPatient();
    if (!patient) {
      this.error.set('Please select a patient.');
      return;
    }
    if (!this.consultingDoctorId) {
      this.error.set('Please select a consulting doctor.');
      return;
    }
    if (this.consultationFeeStatus === 1 && (this.consultationFeeAmount == null || this.consultationFeeAmount < 0)) {
      this.error.set('Consultation fee amount is required when charged.');
      return;
    }

    const discount = this.discountAmount ?? 0;
    const discountReasonText = this.discountReason.trim()
      || (discount > 0 ? this.feeNote.trim() : '');
    if (discount < 0) {
      this.error.set('Invalid discount amount.');
      return;
    }
    if (discount > this.grossSubtotal) {
      this.error.set('Discount cannot exceed the visit subtotal.');
      return;
    }
    if (discount > 0 && !discountReasonText) {
      this.error.set('Please enter a discount reason in the field below Discount (₹).');
      return;
    }

    const collectionAmount = this.collectAtCreate ? (this.initialCollectionAmount ?? 0) : 0;
    if (this.collectAtCreate) {
      if (collectionAmount <= 0) {
        this.error.set('Collection amount must be greater than zero.');
        return;
      }
      if (collectionAmount > this.totalDue) {
        this.error.set('Collection amount cannot exceed total due.');
        return;
      }
      if (!this.collectedByUserId) {
        this.error.set('Please select who collected the payment.');
        return;
      }
    }

    this.saving.set(true);
    this.error.set('');

    const body: Record<string, unknown> = {
      patientId: patient.id,
      consultingDoctorId: this.consultingDoctorId,
      visitType: this.visitType,
      purpose: this.purpose.trim() || null,
      visitNotes: this.visitNotes.trim() || null,
      procedureChargeAmount: this.procedureChargeAmount && this.procedureChargeAmount > 0
        ? this.procedureChargeAmount : null,
      scheduledSurgeryDate: this.scheduledSurgeryDate || null,
      feeStatus: this.consultationFeeStatus,
      consultationFeeAmount: this.consultationFeeStatus === 1 ? this.consultationFeeAmount : null,
      feeNote: this.feeNote.trim() || null,
      initialCollectionAmount: this.collectAtCreate && collectionAmount > 0 ? collectionAmount : null,
      collectedByUserId: this.collectAtCreate && collectionAmount > 0 ? this.collectedByUserId : null,
      paymentMethod: this.collectAtCreate && collectionAmount > 0 ? this.paymentMethod : null,
      addonIds: [...this.selectedAddonIds()],
      discountAmount: discount > 0 ? discount : null,
      discountReason: discount > 0 ? discountReasonText : null
    };

    this.api.post<ApiResult<{ id: string }>>('/visits', body).subscribe({
      next: res => {
        const id = res.data?.id;
        this.router.navigate(id ? ['/visits', id] : ['/visits']);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to record visit.');
        this.saving.set(false);
      }
    });
  }
}
