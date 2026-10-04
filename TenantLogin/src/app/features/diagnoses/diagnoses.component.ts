import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';
import {
  CATHETER_OPTIONS,
  DISCHARGE_TYPE_OPTIONS,
  DischargeMedicine,
  DischargeProcedureKey,
  emptyMedicine,
  FOLLOW_UP_OPTIONS,
  MEDICINE_FREQUENCY_OPTIONS,
  PROCEDURE_OPTIONS,
  procedureFieldDefs,
  SPECIMEN_OPTIONS,
  commonMedicinePack
} from '../admissions/discharge-summary.helpers';

interface DiagnosisListItem {
  id: string;
  code: string;
  name: string;
  procedureKey?: string | null;
  followUpKey?: string | null;
  isActive: boolean;
  sortOrder: number;
  updatedAt: string;
}

interface DiagnosisListResponse {
  items: DiagnosisListItem[];
  totalCount: number;
}

interface DiagnosisDetail {
  id: string;
  code: string;
  name: string;
  pack: Record<string, unknown>;
  isActive: boolean;
  sortOrder: number;
}

interface PackForm {
  chiefComplaints: string;
  briefHistory: string;
  advice: string;
  treatmentDuringAdmission: string;
  procedureKey: DischargeProcedureKey;
  procedureExtras: Record<string, string>;
  anaesthesia: string;
  indication: string;
  catheterKey: string;
  catheterOther: string;
  specimen: string;
  dischargeType: string;
  followUpKey: string;
  followUpOther: string;
  followUpNotes: string;
  generalCondition: string;
  postopCourse: string;
  pastHistory: {
    comorbidities: string;
    pastSurgery: string;
    allergy: string;
    addiction: string;
  };
  medicinePackSize: number | null;
  medicines: DischargeMedicine[];
}

function emptyPack(): PackForm {
  return {
    chiefComplaints: '',
    briefHistory: '',
    advice: '',
    treatmentDuringAdmission: '',
    procedureKey: '',
    procedureExtras: {},
    anaesthesia: 'Spinal anaesthesia',
    indication: '',
    catheterKey: '',
    catheterOther: '',
    specimen: 'Not applicable',
    dischargeType: 'Routine discharge',
    followUpKey: '',
    followUpOther: '',
    followUpNotes: '',
    generalCondition: 'Stable',
    postopCourse: 'Uneventful',
    pastHistory: {
      comorbidities: 'Nil',
      pastSurgery: 'Nil',
      allergy: 'Nil',
      addiction: 'Nil'
    },
    medicinePackSize: null,
    medicines: commonMedicinePack().map(m => ({ ...m }))
  };
}

function packFromApi(raw: Record<string, unknown> | null | undefined): PackForm {
  const base = emptyPack();
  if (!raw) return base;
  const past = (raw['pastHistory'] as Record<string, string>) ?? {};
  const meds = Array.isArray(raw['medicines'])
    ? (raw['medicines'] as DischargeMedicine[]).map(m => ({
        id: m.id || emptyMedicine().id,
        name: m.name ?? '',
        strength: m.strength ?? '',
        dose: m.dose ?? '',
        route: m.route ?? 'Oral',
        frequency: m.frequency ?? '',
        duration: m.duration ?? '',
        timing: m.timing ?? ''
      }))
    : base.medicines;

  return {
    chiefComplaints: String(raw['chiefComplaints'] ?? ''),
    briefHistory: String(raw['briefHistory'] ?? ''),
    advice: String(raw['advice'] ?? ''),
    treatmentDuringAdmission: String(raw['treatmentDuringAdmission'] ?? ''),
    procedureKey: (raw['procedureKey'] as DischargeProcedureKey) || '',
    procedureExtras: { ...((raw['procedureExtras'] as Record<string, string>) ?? {}) },
    anaesthesia: String(raw['anaesthesia'] ?? base.anaesthesia),
    indication: String(raw['indication'] ?? ''),
    catheterKey: String(raw['catheterKey'] ?? ''),
    catheterOther: String(raw['catheterOther'] ?? ''),
    specimen: String(raw['specimen'] ?? base.specimen),
    dischargeType: String(raw['dischargeType'] ?? base.dischargeType),
    followUpKey: String(raw['followUpKey'] ?? ''),
    followUpOther: String(raw['followUpOther'] ?? ''),
    followUpNotes: String(raw['followUpNotes'] ?? ''),
    generalCondition: String(raw['generalCondition'] ?? base.generalCondition),
    postopCourse: String(raw['postopCourse'] ?? base.postopCourse),
    pastHistory: {
      comorbidities: String(past['comorbidities'] ?? 'Nil'),
      pastSurgery: String(past['pastSurgery'] ?? 'Nil'),
      allergy: String(past['allergy'] ?? 'Nil'),
      addiction: String(past['addiction'] ?? 'Nil')
    },
    medicinePackSize: raw['medicinePackSize'] == null ? null : Number(raw['medicinePackSize']),
    medicines: meds
  };
}

@Component({
  selector: 'app-diagnoses',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './diagnoses.component.html',
  styleUrl: './diagnoses.component.scss'
})
export class DiagnosesComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly rows = signal<DiagnosisListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly drawerOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly formError = signal('');
  readonly updatingStatusId = signal<string | null>(null);
  readonly deletingId = signal<string | null>(null);

  readonly catheterOptions = CATHETER_OPTIONS;
  readonly specimenOptions = SPECIMEN_OPTIONS;
  readonly dischargeTypeOptions = DISCHARGE_TYPE_OPTIONS;
  readonly followUpOptions = FOLLOW_UP_OPTIONS;
  readonly procedureOptions = PROCEDURE_OPTIONS;
  readonly frequencyOptions = MEDICINE_FREQUENCY_OPTIONS;

  statusFilter: boolean | null = null;
  name = '';
  isActive = true;
  sortOrder = 0;
  pack = emptyPack();

  ngOnInit() {
    this.loadList();
  }

  procedureFields() {
    return procedureFieldDefs(this.pack.procedureKey);
  }

  loadList() {
    this.loading.set(true);
    this.error.set('');
    const query = new URLSearchParams({ page: '1', pageSize: '100' });
    if (this.statusFilter != null) query.set('isActive', String(this.statusFilter));

    this.api.get<ApiResult<DiagnosisListResponse>>(`/diagnosis-masters?${query}`).subscribe({
      next: res => {
        this.rows.set(res.data?.items ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load diagnoses.');
        this.loading.set(false);
      }
    });
  }

  onStatusFilterChange() {
    this.loadList();
  }

  openCreate() {
    this.editingId.set(null);
    this.name = '';
    this.isActive = true;
    this.sortOrder = 0;
    this.pack = emptyPack();
    this.formError.set('');
    this.drawerOpen.set(true);
  }

  openEdit(row: DiagnosisListItem) {
    this.formError.set('');
    this.api.get<ApiResult<DiagnosisDetail>>(`/diagnosis-masters/${row.id}`).subscribe({
      next: res => {
        const d = res.data;
        if (!d) {
          this.formError.set('Diagnosis not found.');
          return;
        }
        this.editingId.set(d.id);
        this.name = d.name;
        this.isActive = d.isActive;
        this.sortOrder = d.sortOrder;
        this.pack = packFromApi(d.pack);
        this.drawerOpen.set(true);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load diagnosis.');
      }
    });
  }

  closeDrawer() {
    if (this.saving()) return;
    this.drawerOpen.set(false);
    this.formError.set('');
  }

  onProcedureChange() {
    this.pack.procedureExtras = {};
  }

  setExtra(key: string, value: string) {
    this.pack.procedureExtras = { ...this.pack.procedureExtras, [key]: value };
  }

  addMedicine() {
    this.pack.medicines = [...this.pack.medicines, emptyMedicine()];
  }

  removeMedicine(id: string) {
    this.pack.medicines = this.pack.medicines.filter(m => m.id !== id);
  }

  submit() {
    if (!this.name.trim()) {
      this.formError.set('Diagnosis name is required.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');

    const packPayload = {
      chiefComplaints: this.pack.chiefComplaints,
      briefHistory: this.pack.briefHistory,
      advice: this.pack.advice,
      treatmentDuringAdmission: this.pack.treatmentDuringAdmission,
      procedureKey: this.pack.procedureKey,
      procedureExtras: this.pack.procedureExtras,
      anaesthesia: this.pack.anaesthesia,
      indication: this.pack.indication.trim() || this.name.trim(),
      catheterKey: this.pack.catheterKey,
      catheterOther: this.pack.catheterOther,
      specimen: this.pack.specimen,
      dischargeType: this.pack.dischargeType,
      followUpKey: this.pack.followUpKey,
      followUpOther: this.pack.followUpOther,
      followUpNotes: this.pack.followUpNotes,
      generalCondition: this.pack.generalCondition,
      postopCourse: this.pack.postopCourse,
      pastHistory: this.pack.pastHistory,
      medicinePackSize: this.pack.medicinePackSize,
      medicines: this.pack.medicines.filter(m => m.name.trim())
    };

    const body = {
      name: this.name.trim(),
      pack: packPayload,
      sortOrder: this.sortOrder,
      isActive: this.isActive
    };

    const editId = this.editingId();
    const request = editId
      ? this.api.put<ApiResult<DiagnosisDetail>>(`/diagnosis-masters/${editId}`, body)
      : this.api.post<ApiResult<DiagnosisDetail>>('/diagnosis-masters', body);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.loadList();
      },
      error: err => {
        this.formError.set(err.error?.message ?? 'Unable to save diagnosis.');
        this.saving.set(false);
      }
    });
  }

  toggleStatus(row: DiagnosisListItem) {
    this.updatingStatusId.set(row.id);
    this.error.set('');
    const next = !row.isActive;
    this.api.patch<ApiResult<boolean>>(`/diagnosis-masters/${row.id}/status?isActive=${next}`, {}).subscribe({
      next: () => {
        this.updatingStatusId.set(null);
        this.loadList();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to update status.');
        this.updatingStatusId.set(null);
      }
    });
  }

  deleteRow(row: DiagnosisListItem) {
    if (!confirm(`Delete ${row.name}?`)) return;
    this.deletingId.set(row.id);
    this.error.set('');
    this.api.delete<ApiResult<boolean>>(`/diagnosis-masters/${row.id}`).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.loadList();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to delete diagnosis.');
        this.deletingId.set(null);
      }
    });
  }
}
