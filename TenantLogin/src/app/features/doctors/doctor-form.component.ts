import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';

interface DoctorDetail {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  status: string;
  statusCode: number;
}

@Component({
  selector: 'app-doctor-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './doctor-form.component.html',
  styleUrl: './doctor-form.component.scss'
})
export class DoctorFormComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly isEdit = signal(false);
  doctorId = '';

  firstName = '';
  lastName = '';
  email = '';
  temporaryPassword = '';

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
      this.doctorId = id;
      this.loadDoctor(id);
    }
  }

  loadDoctor(id: string) {
    this.loading.set(true);
    this.api.get<ApiResult<DoctorDetail>>(`/doctors/${id}`).subscribe({
      next: res => {
        const d = res.data;
        if (!d) {
          this.error.set('Doctor not found.');
          this.loading.set(false);
          return;
        }
        this.firstName = d.firstName;
        this.lastName = d.lastName;
        this.email = d.email;
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load doctor.');
        this.loading.set(false);
      }
    });
  }

  submit() {
    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.error.set('First name and last name are required.');
      return;
    }
    if (!this.isEdit() && (!this.email.trim() || !this.email.includes('@'))) {
      this.error.set('A valid email is required.');
      return;
    }

    this.saving.set(true);
    this.error.set('');

    if (this.isEdit()) {
      this.api.put<ApiResult<DoctorDetail>>(`/doctors/${this.doctorId}`, {
        firstName: this.firstName.trim(),
        lastName: this.lastName.trim()
      }).subscribe({
        next: () => this.router.navigate(['/doctors']),
        error: err => {
          this.error.set(err.error?.message ?? 'Unable to update doctor.');
          this.saving.set(false);
        }
      });
      return;
    }

    const body: Record<string, string> = {
      email: this.email.trim(),
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim()
    };
    if (this.temporaryPassword.trim()) {
      body['temporaryPassword'] = this.temporaryPassword.trim();
    }

    this.api.post<ApiResult<DoctorDetail>>('/doctors', body).subscribe({
      next: () => this.router.navigate(['/doctors']),
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to create doctor.');
        this.saving.set(false);
      }
    });
  }
}
