import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';

interface PatientListItem {
  id: string;
  patientCode: string;
  firstName: string;
  lastName: string;
  phone: string;
  gender: string;
  status: string;
  statusCode: number;
  createdAt: string;
}

interface PatientList {
  items: PatientListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [FormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './patients.component.html',
  styleUrl: './patients.component.scss'
})
export class PatientsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly patients = signal<PatientListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  searchTerm = '';
  readonly Math = Math;

  readonly canEdit = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly confirmTarget = signal<PatientListItem | null>(null);

  readonly confirmMessage = computed(() => {
    const patient = this.confirmTarget();
    if (!patient) return '';
    const name = `${patient.firstName} ${patient.lastName}`.trim();
    return `Patient ${patient.patientCode} (${name}) will be removed from the list. The record is kept in the system (soft delete).`;
  });

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canEdit.set(user?.role === 'TenantSuperAdmin' || user?.role === 'Staff');
    this.loadPatients();
  }

  loadPatients() {
    this.loading.set(true);
    this.error.set('');

    const term = this.searchTerm.trim();
    const query = new URLSearchParams({
      page: String(this.page()),
      pageSize: String(this.pageSize)
    });

    if (term) {
      const digits = term.replace(/\D/g, '');
      if (digits.length >= 10 && digits.length <= 15 && /^[\d\s\-+()]+$/.test(term)) {
        query.set('phone', digits);
      } else {
        query.set('patientCode', term);
      }
    }

    this.api.get<ApiResult<PatientList>>(`/patients?${query}`).subscribe({
      next: res => {
        this.patients.set(res.data?.items ?? []);
        this.totalCount.set(res.data?.totalCount ?? 0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load patients.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.loadPatients();
  }

  clearSearch() {
    this.searchTerm = '';
    this.page.set(1);
    this.loadPatients();
  }

  prevPage() {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.loadPatients();
    }
  }

  nextPage() {
    const maxPage = Math.ceil(this.totalCount() / this.pageSize);
    if (this.page() < maxPage) {
      this.page.update(p => p + 1);
      this.loadPatients();
    }
  }

  deletePatient(patient: PatientListItem) {
    this.confirmTarget.set(patient);
  }

  cancelDelete() {
    if (!this.deletingId()) {
      this.confirmTarget.set(null);
    }
  }

  confirmDelete() {
    const patient = this.confirmTarget();
    if (!patient) return;

    this.deletingId.set(patient.id);
    this.error.set('');

    this.api.delete<ApiResult<boolean>>(`/patients/${patient.id}`).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.confirmTarget.set(null);
        this.loadPatients();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to delete patient.');
        this.deletingId.set(null);
      }
    });
  }
}
