import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface DoctorListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  departmentId?: string | null;
  departmentName?: string | null;
  status: string;
  statusCode: number;
  lastLoginAt?: string | null;
  createdAt: string;
}

interface DoctorDetail {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  departmentId?: string | null;
  departmentName?: string | null;
  status: string;
  statusCode: number;
}

interface DoctorList {
  items: DoctorListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface DepartmentLookup {
  id: string;
  name: string;
}

@Component({
  selector: 'app-doctors',
  standalone: true,
  imports: [FormsModule, DatePipe],
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

  readonly drawerOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly formError = signal('');
  readonly departments = signal<DepartmentLookup[]>([]);

  searchTerm = '';
  statusFilter: number | null = null;
  firstName = '';
  lastName = '';
  email = '';
  temporaryPassword = '';
  departmentId = '';

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canManage.set(user?.role === 'TenantSuperAdmin');
    this.loadDoctors();
    if (this.canManage()) {
      this.loadDepartments();
    }
  }

  isEditing(): boolean {
    return !!this.editingId();
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

  loadDepartments() {
    this.api.get<ApiResult<DepartmentLookup[]>>('/departments/active').subscribe({
      next: res => this.departments.set(res.data ?? []),
      error: () => this.formError.set('Unable to load departments.')
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

  openCreate() {
    if (!this.canManage()) return;
    this.editingId.set(null);
    this.firstName = '';
    this.lastName = '';
    this.email = '';
    this.temporaryPassword = '';
    this.departmentId = '';
    this.formError.set('');
    this.drawerOpen.set(true);
    if (!this.departments().length) this.loadDepartments();
  }

  openEdit(doctor: DoctorListItem) {
    if (!this.canManage()) return;
    this.formError.set('');
    this.api.get<ApiResult<DoctorDetail>>(`/doctors/${doctor.id}`).subscribe({
      next: res => {
        const d = res.data;
        if (!d) {
          this.error.set('Doctor not found.');
          return;
        }
        this.editingId.set(d.id);
        this.firstName = d.firstName;
        this.lastName = d.lastName;
        this.email = d.email;
        this.temporaryPassword = '';
        this.departmentId = d.departmentId ?? '';
        this.ensureCurrentDepartmentOption(d.departmentId, d.departmentName);
        this.drawerOpen.set(true);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load doctor.');
      }
    });
  }

  closeDrawer() {
    if (this.saving()) return;
    this.drawerOpen.set(false);
    this.formError.set('');
  }

  submit() {
    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.formError.set('First name and last name are required.');
      return;
    }
    if (!this.departmentId) {
      this.formError.set('Department is required.');
      return;
    }
    if (!this.isEditing() && (!this.email.trim() || !this.email.includes('@'))) {
      this.formError.set('A valid email is required.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');

    const editId = this.editingId();
    if (editId) {
      this.api.put<ApiResult<DoctorDetail>>(`/doctors/${editId}`, {
        firstName: this.firstName.trim(),
        lastName: this.lastName.trim(),
        departmentId: this.departmentId
      }).subscribe({
        next: () => {
          this.saving.set(false);
          this.drawerOpen.set(false);
          this.loadDoctors();
        },
        error: err => {
          this.formError.set(err.error?.message ?? 'Unable to update doctor.');
          this.saving.set(false);
        }
      });
      return;
    }

    const body: Record<string, string> = {
      email: this.email.trim(),
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      departmentId: this.departmentId
    };
    if (this.temporaryPassword.trim()) {
      body['temporaryPassword'] = this.temporaryPassword.trim();
    }

    this.api.post<ApiResult<DoctorDetail>>('/doctors', body).subscribe({
      next: () => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.page.set(1);
        this.loadDoctors();
      },
      error: err => {
        this.formError.set(err.error?.message ?? 'Unable to create doctor.');
        this.saving.set(false);
      }
    });
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

  private ensureCurrentDepartmentOption(id: string | null | undefined, name: string | null | undefined) {
    if (!id) return;
    const list = this.departments();
    if (list.some(d => d.id === id)) return;
    this.departments.set([
      ...list,
      { id, name: name?.trim() || 'Current department (inactive)' }
    ]);
  }
}
