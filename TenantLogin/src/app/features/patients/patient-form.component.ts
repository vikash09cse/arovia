import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';
import { DobPickerComponent } from '../../shared/dob-picker/dob-picker.component';

interface PatientDetail {
  id: string;
  patientCode: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  age?: number | null;
  genderCode: number;
  phone: string;
  email?: string;
  address: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  bloodGroupCode?: number;
  referredBy?: string;
}

@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [FormsModule, RouterLink, DobPickerComponent],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.scss'
})
export class PatientFormComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly isEdit = signal(false);
  patientId = '';

  firstName = '';
  lastName = '';
  dateOfBirth = '';
  age: number | null = null;
  gender = 1;
  phone = '';
  email = '';
  address = '';
  emergencyContactName = '';
  emergencyContactPhone = '';
  bloodGroup: number | null = null;
  referredBy = '';

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
      this.patientId = id;
      this.loadPatient(id);
    }
  }

  loadPatient(id: string) {
    this.loading.set(true);
    this.api.get<ApiResult<PatientDetail>>(`/patients/${id}`).subscribe({
      next: res => {
        const p = res.data;
        if (!p) {
          this.error.set('Patient not found.');
          this.loading.set(false);
          return;
        }
        this.firstName = p.firstName;
        this.lastName = p.lastName;
        this.dateOfBirth = p.dateOfBirth ? p.dateOfBirth.slice(0, 10) : '';
        this.age = p.age ?? null;
        this.gender = p.genderCode;
        this.phone = p.phone;
        this.email = p.email ?? '';
        this.address = p.address;
        this.emergencyContactName = p.emergencyContactName ?? '';
        this.emergencyContactPhone = p.emergencyContactPhone ?? '';
        this.bloodGroup = p.bloodGroupCode ?? null;
        this.referredBy = p.referredBy ?? '';
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load patient.');
        this.loading.set(false);
      }
    });
  }

  blockDecimal(event: KeyboardEvent) {
    if (event.key === '.' || event.key === ',' || event.key === 'e' || event.key === 'E' || event.key === '-' || event.key === '+') {
      event.preventDefault();
    }
  }

  sanitizeAge() {
    if (this.age == null || Number.isNaN(this.age)) {
      this.age = null;
      return;
    }
    this.age = Math.trunc(Math.max(0, Math.min(150, this.age)));
  }

  submit() {
    const hasDob = !!this.dateOfBirth?.trim();
    const hasAge = this.age != null && !Number.isNaN(this.age);

    if (!hasDob && !hasAge) {
      this.error.set('Please enter date of birth or age.');
      return;
    }

    this.saving.set(true);
    this.error.set('');

    const body = {
      firstName: this.firstName,
      lastName: this.lastName,
      dateOfBirth: hasDob ? this.dateOfBirth : null,
      age: hasAge ? this.age : null,
      gender: this.gender,
      phone: this.phone,
      email: this.email || null,
      address: this.address,
      emergencyContactName: this.emergencyContactName.trim() || null,
      emergencyContactPhone: this.emergencyContactPhone.trim() || null,
      bloodGroup: this.bloodGroup,
      referredBy: this.referredBy || null
    };

    const request$ = this.isEdit()
      ? this.api.put<ApiResult<PatientDetail>>(`/patients/${this.patientId}`, body)
      : this.api.post<ApiResult<PatientDetail>>('/patients', body);

    request$.subscribe({
      next: () => this.router.navigate(['/patients']),
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to save patient.');
        this.saving.set(false);
      }
    });
  }
}
