import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';
import {
  applyDiagnosisPack,
  CATHETER_OPTIONS,
  commonMedicinePack,
  composeDischargeForm,
  DiagnosisMaster,
  DISCHARGE_TYPE_OPTIONS,
  DischargeForm,
  DischargeProcedureKey,
  emptyMedicine,
  FOLLOW_UP_OPTIONS,
  INVESTIGATION_ROWS,
  MEDICINE_FREQUENCY_OPTIONS,
  normalizeForm,
  PROCEDURE_OPTIONS,
  procedureFieldDefs,
  SPECIMEN_OPTIONS
} from './discharge-summary.helpers';

interface AdmissionHeader {
  admissionId: string;
  admissionCode: string;
  admittedAt: string;
  dischargedAt?: string | null;
  ward: string;
  bed?: string | null;
  roomClass: string;
  status: string;
  statusCode: number;
  patientId: string;
  patientCode: string;
  patientFullName: string;
  patientAge?: number | null;
  patientGenderLabel: string;
  doctorName: string;
  doctorDesignation?: string | null;
  departmentName?: string | null;
  hospitalName: string;
}

interface DischargeSummaryApi {
  admission: AdmissionHeader;
  dischargeSummaryId?: string | null;
  exists: boolean;
  dateOfSurgery?: string | null;
  dateOfDischarge?: string | null;
  finalDiagnosis?: string | null;
  diagnosisKey?: string | null;
  formSchemaVersion: number;
  form: any;
}

@Component({
  selector: 'app-discharge-summary',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './discharge-summary.component.html',
  styleUrl: './discharge-summary.component.scss'
})
export class DischargeSummaryComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly printing = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly admission = signal<AdmissionHeader | null>(null);
  readonly diagnoses = signal<DiagnosisMaster[]>([]);
  readonly exists = signal(false);
  readonly showAdvanced = signal(false);

  form: DischargeForm = normalizeForm({}, '');
  formSchemaVersion = 1;

  readonly investigationRows = INVESTIGATION_ROWS;
  readonly procedureOptions = PROCEDURE_OPTIONS;
  readonly catheterOptions = CATHETER_OPTIONS;
  readonly specimenOptions = SPECIMEN_OPTIONS;
  readonly dischargeTypeOptions = DISCHARGE_TYPE_OPTIONS;
  readonly followUpOptions = FOLLOW_UP_OPTIONS;
  readonly frequencyOptions = MEDICINE_FREQUENCY_OPTIONS;

  admissionId = '';

  ngOnInit() {
    this.admissionId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.admissionId) {
      this.error.set('Admission not found.');
      this.loading.set(false);
      return;
    }
    this.load();
  }

  get procedureFields() {
    return procedureFieldDefs(this.form.procedureKey);
  }

  showLegacyDiagnosisOption(): boolean {
    const key = this.form.diagnosisKey;
    if (!key || key === 'Other') return false;
    return !this.diagnoses().some(d => d.name === key);
  }

  ageSexLabel(): string {
    const a = this.admission();
    if (!a) return '—';
    const age = a.patientAge != null ? `${a.patientAge}y` : '—';
    return `${age} / ${a.patientGenderLabel}`;
  }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.api.get<ApiResult<DischargeSummaryApi>>(`/admissions/${this.admissionId}/discharge-summary`).subscribe({
      next: res => {
        const data = res.data;
        if (!data) {
          this.error.set(res.message || 'Unable to load discharge summary.');
          this.loading.set(false);
          return;
        }
        this.admission.set(data.admission);
        this.exists.set(data.exists);
        this.formSchemaVersion = data.formSchemaVersion || 1;
        this.form = normalizeForm(data.form, data.admission.admissionId);
        if (data.dateOfSurgery) this.form.dateOfSurgery = String(data.dateOfSurgery).slice(0, 10);
        if (data.dateOfDischarge) this.form.dateOfDischarge = String(data.dateOfDischarge).slice(0, 10);
        this.loading.set(false);
        this.loadDiagnoses();
      },
      error: err => {
        this.error.set(err?.error?.message || 'Unable to load discharge summary.');
        this.loading.set(false);
      }
    });
  }

  loadDiagnoses() {
    this.api.get<ApiResult<{ items: { id: string; code: string; name: string; pack: Record<string, unknown> }[] }>>(
      '/diagnosis-masters/active'
    ).subscribe({
      next: res => {
        const items = (res.data?.items ?? []).map(x => ({
          id: x.id,
          code: x.code,
          name: x.name,
          pack: x.pack ?? {}
        }));
        this.diagnoses.set(items);
      }
    });
  }

  onDiagnosisChange(value: string) {
    if (value === 'Other') {
      this.form.diagnosisKey = 'Other';
      this.form.finalDiagnosis = this.form.diagnosisOther;
      return;
    }
    if (!value) {
      this.form.diagnosisKey = '';
      this.form.finalDiagnosis = '';
      return;
    }
    const master = this.diagnoses().find(d => d.name === value);
    if (master) {
      this.form = applyDiagnosisPack(this.form, master);
    } else {
      this.form.diagnosisKey = value;
      this.form.finalDiagnosis = value;
    }
  }

  onProcedureChange(key: string) {
    this.form.procedureKey = key as DischargeProcedureKey;
    this.form.procedureExtras = {};
  }

  applyCompose() {
    this.form = composeDischargeForm(this.form);
    this.message.set('Operative / condition text updated from guided fields.');
  }

  addMedicine() {
    this.form.medicines = [...this.form.medicines, emptyMedicine()];
  }

  loadCommonPack() {
    this.form.medicines = commonMedicinePack().map((m, i) => ({
      ...m,
      id: `pack_${Date.now()}_${i}`
    }));
    this.message.set('Common medicine pack loaded.');
  }

  removeMedicine(index: number) {
    this.form.medicines = this.form.medicines.filter((_, i) => i !== index);
  }

  save() {
    this.saving.set(true);
    this.error.set('');
    this.message.set('');
    const composed = composeDischargeForm(this.form);
    this.form = composed;

    const body = {
      dateOfSurgery: composed.dateOfSurgery || null,
      dateOfDischarge: composed.dateOfDischarge || null,
      finalDiagnosis: composed.finalDiagnosis || null,
      diagnosisKey: composed.diagnosisKey || null,
      formSchemaVersion: this.formSchemaVersion || 1,
      form: composed
    };

    this.api.put<ApiResult<DischargeSummaryApi>>(
      `/admissions/${this.admissionId}/discharge-summary`,
      body
    ).subscribe({
      next: res => {
        this.saving.set(false);
        if (!res.data) {
          this.error.set(res.message || 'Save failed.');
          return;
        }
        this.exists.set(res.data.exists);
        this.form = normalizeForm(res.data.form, res.data.admission.admissionId);
        if (res.data.dateOfSurgery) this.form.dateOfSurgery = String(res.data.dateOfSurgery).slice(0, 10);
        if (res.data.dateOfDischarge) this.form.dateOfDischarge = String(res.data.dateOfDischarge).slice(0, 10);
        this.message.set('Discharge summary saved.');
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message || 'Save failed.');
      }
    });
  }

  async print() {
    this.printing.set(true);
    this.error.set('');
    try {
      // Save first so PDF reflects latest form data
      await new Promise<void>((resolve, reject) => {
        const composed = composeDischargeForm(this.form);
        this.form = composed;
        this.api.put<ApiResult<DischargeSummaryApi>>(
          `/admissions/${this.admissionId}/discharge-summary`,
          {
            dateOfSurgery: composed.dateOfSurgery || null,
            dateOfDischarge: composed.dateOfDischarge || null,
            finalDiagnosis: composed.finalDiagnosis || null,
            diagnosisKey: composed.diagnosisKey || null,
            formSchemaVersion: this.formSchemaVersion || 1,
            form: composed
          }
        ).subscribe({
          next: res => {
            if (!res.data) reject(new Error(res.message || 'Save failed.'));
            else {
              this.exists.set(true);
              resolve();
            }
          },
          error: err => reject(new Error(err?.error?.message || 'Save failed.'))
        });
      });

      const response = await firstValueFrom(
        this.api.getBlob(`/admissions/${this.admissionId}/discharge-summary/print.pdf`)
      );
      const blob = response.body;
      if (!blob) throw new Error('PDF download failed.');

      if (blob.type.includes('application/json')) {
        const text = await blob.text();
        try {
          const parsed = JSON.parse(text) as { message?: string };
          throw new Error(parsed.message || 'Unable to generate PDF.');
        } catch (e) {
          if (e instanceof SyntaxError) throw new Error('Unable to generate PDF.');
          throw e;
        }
      }

      const url = URL.createObjectURL(blob);
      const iframe = document.createElement('iframe');
      iframe.setAttribute('aria-hidden', 'true');
      iframe.setAttribute('title', 'Print discharge summary');
      Object.assign(iframe.style, {
        position: 'fixed',
        right: '0',
        bottom: '0',
        width: '0',
        height: '0',
        border: '0',
        opacity: '0',
        pointerEvents: 'none'
      });
      document.body.appendChild(iframe);

      await new Promise<void>((resolve, reject) => {
        iframe.onload = () => resolve();
        iframe.onerror = () => reject(new Error('Unable to load PDF for print.'));
        iframe.src = url;
        // Some browsers fire load late for PDFs; safety resolve
        setTimeout(() => resolve(), 800);
      });

      const win = iframe.contentWindow;
      if (!win) {
        iframe.remove();
        URL.revokeObjectURL(url);
        throw new Error('Unable to prepare print view.');
      }

      const cleanup = () => {
        try { iframe.remove(); } catch { /* ignore */ }
        URL.revokeObjectURL(url);
      };
      win.addEventListener('afterprint', cleanup, { once: true });
      setTimeout(cleanup, 60_000);
      win.focus();
      win.print();
      this.message.set('Discharge summary PDF ready to print.');
    } catch (e: any) {
      this.error.set(e?.message || 'Print failed.');
    } finally {
      this.printing.set(false);
    }
  }
}
