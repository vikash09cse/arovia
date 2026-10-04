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

interface DepartmentLookup {
  id: string;
  name: string;
}

@Component({
  selector: 'app-admission-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './admission-form.component.html',
  styleUrl: './admission-form.component.scss'
})
export class AdmissionFormComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly saving = signal(false);
  readonly error = signal('');
  readonly departments = signal<DepartmentLookup[]>([]);
  readonly doctors = signal<LookupItem[]>([]);
  readonly collectors = signal<LookupItem[]>([]);
  readonly selectedPatient = signal<PatientListItem | null>(null);
  readonly loadingDoctors = signal(false);

  patientSearch = '';
  departmentId = '';
  attendingDoctorId = '';
  ward = 'General';
  bed = '';
  roomClass = 'Semi-private';
  estimatedAmount: number | null = null;
  notes = '';
  depositAmount: number | null = null;
  paymentMethod = 1;
  collectedByUserId = '';
  fromVisitId: string | null = null;

  ngOnInit() {
    const user = this.auth.currentUser();
    if (user?.userId) this.collectedByUserId = user.userId;

    this.loadDepartments();
    this.loadCollectors();

    const patientId = this.route.snapshot.queryParamMap.get('patientId');
    this.fromVisitId = this.route.snapshot.queryParamMap.get('fromVisitId');
    if (patientId) this.loadPatientById(patientId);
  }

  loadDepartments() {
    this.api.get<ApiResult<DepartmentLookup[]>>('/departments/active').subscribe({
      next: res => {
        const list = res.data ?? [];
        this.departments.set(list);
        if (list.length === 1) {
          this.departmentId = list[0].id;
          this.onDepartmentChange();
        }
      },
      error: () => this.error.set('Unable to load departments.')
    });
  }

  onDepartmentChange() {
    this.attendingDoctorId = '';
    this.doctors.set([]);
    if (!this.departmentId) return;
    this.loadDoctors(this.departmentId);
  }

  loadDoctors(departmentId: string) {
    this.loadingDoctors.set(true);
    const query = new URLSearchParams({ departmentId });
    this.api.get<ApiResult<LookupItem[]>>(`/doctors/active?${query}`).subscribe({
      next: res => {
        const list = res.data ?? [];
        this.doctors.set(list);
        if (list.length === 1) this.attendingDoctorId = list[0].id;
        this.loadingDoctors.set(false);
      },
      error: () => {
        this.error.set('Unable to load doctors for this department.');
        this.loadingDoctors.set(false);
      }
    });
  }

  loadCollectors() {
    this.api.get<ApiResult<LookupItem[]>>('/visits/payment-collectors').subscribe({
      next: res => this.collectors.set(res.data ?? []),
      error: () => { /* optional */ }
    });
  }

  loadPatientById(id: string) {
    this.api.get<ApiResult<PatientListItem>>(`/patients/${id}`).subscribe({
      next: res => {
        if (res.data) this.selectedPatient.set(res.data);
      },
      error: () => this.error.set('Unable to load patient.')
    });
  }

  searchPatients() {
    const term = this.patientSearch.trim();
    if (!term) return;
    const query = new URLSearchParams({ page: '1', pageSize: '10' });
    if (this.isPhoneSearchTerm(term)) query.set('phone', term.replace(/\D/g, ''));
    else query.set('patientCode', term);

    this.api.get<ApiResult<PatientList>>(`/patients?${query}`).subscribe({
      next: res => {
        const items = res.data?.items ?? [];
        if (items.length === 1) {
          this.selectedPatient.set(items[0]);
          this.error.set('');
        } else if (items.length === 0) {
          this.selectedPatient.set(null);
          this.error.set('No patient found.');
        } else {
          this.error.set('Multiple patients found — refine your search.');
        }
      },
      error: err => this.error.set(err.error?.message ?? 'Unable to search patients.')
    });
  }

  /** Phone if digits-only (optional formatting); codes like 20260905-01 stay patientCode. */
  private isPhoneSearchTerm(term: string): boolean {
    const digits = term.replace(/\D/g, '');
    if (digits.length < 10 || digits.length > 15) return false;
    if (/[a-zA-Z]/.test(term)) return false;
    // Patient codes: YYYYMMDD-NN (or similar prefix-seq with a hyphen)
    if (/^\d{6,}-\d+$/.test(term)) return false;
    return true;
  }

  clearPatient() {
    this.selectedPatient.set(null);
    this.patientSearch = '';
  }

  submit() {
    const patient = this.selectedPatient();
    if (!patient) {
      this.error.set('Select a patient.');
      return;
    }
    if (!this.departmentId) {
      this.error.set('Select a department.');
      return;
    }
    if (!this.attendingDoctorId) {
      this.error.set('Select an attending doctor.');
      return;
    }
    if (!this.ward.trim() || !this.roomClass.trim()) {
      this.error.set('Ward and room class are required.');
      return;
    }

    const deposit = this.depositAmount ?? 0;
    if (deposit > 0 && !this.collectedByUserId) {
      this.error.set('Select who collected the deposit.');
      return;
    }

    this.saving.set(true);
    this.error.set('');

    const body: Record<string, unknown> = {
      patientId: patient.id,
      departmentId: this.departmentId,
      attendingDoctorId: this.attendingDoctorId,
      ward: this.ward.trim(),
      bed: this.bed.trim() || null,
      roomClass: this.roomClass.trim(),
      estimatedAmount: this.estimatedAmount ?? 0,
      notes: this.notes.trim() || null,
      fromVisitId: this.fromVisitId || null
    };

    if (deposit > 0) {
      body['depositAmount'] = deposit;
      body['collectedByUserId'] = this.collectedByUserId;
      body['paymentMethod'] = this.paymentMethod;
    }

    this.api.post<ApiResult<{ id: string }>>('/admissions', body).subscribe({
      next: res => {
        this.saving.set(false);
        const id = res.data?.id;
        if (id) this.router.navigate(['/admissions', id]);
        else this.error.set('Admission created but id was missing.');
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err.error?.message ?? 'Unable to admit patient.');
      }
    });
  }
}
