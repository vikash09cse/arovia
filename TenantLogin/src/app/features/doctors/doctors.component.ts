import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface DoctorListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  status: string;
  statusCode: number;
  lastLoginAt?: string | null;
  createdAt: string;
}

interface DoctorList {
  items: DoctorListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-doctors',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './doctors.component.html',
  styleUrl: './doctors.component.scss'
})
export class DoctorsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly doctors = signal<DoctorListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly canManage = signal(false);
  readonly togglingId = signal<string | null>(null);
  readonly Math = Math;

  searchTerm = '';
  statusFilter: number | null = null;

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canManage.set(user?.role === 'TenantSuperAdmin');
    this.loadDoctors();
  }

  loadDoctors() {
    this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams({
      page: String(this.page()),
      pageSize: String(this.pageSize)
    });
    const term = this.searchTerm.trim();
    if (term) query.set('filter', term);
    if (this.statusFilter != null) query.set('status', String(this.statusFilter));

    this.api.get<ApiResult<DoctorList>>(`/doctors?${query}`).subscribe({
      next: res => {
        this.doctors.set(res.data?.items ?? []);
        this.totalCount.set(res.data?.totalCount ?? 0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load doctors.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.loadDoctors();
  }

  clearSearch() {
    this.searchTerm = '';
    this.statusFilter = null;
    this.page.set(1);
    this.loadDoctors();
  }

  prevPage() {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.loadDoctors();
    }
  }

  nextPage() {
    const maxPage = Math.ceil(this.totalCount() / this.pageSize);
    if (this.page() < maxPage) {
      this.page.update(p => p + 1);
      this.loadDoctors();
    }
  }

  toggleStatus(doctor: DoctorListItem) {
    if (!this.canManage()) return;

    const nextStatus = doctor.statusCode === 1 ? 2 : 1;
    this.togglingId.set(doctor.id);
    this.error.set('');

    this.api.patch<ApiResult<boolean>>(`/doctors/${doctor.id}/status?status=${nextStatus}`).subscribe({
      next: () => {
        this.togglingId.set(null);
        this.loadDoctors();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to update doctor status.');
        this.togglingId.set(null);
      }
    });
  }
}
