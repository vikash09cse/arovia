import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface Department {
  id: string;
  name: string;
  status: string;
  statusCode: number;
}

interface DepartmentList {
  items: Department[];
  totalCount: number;
}

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './departments.component.html',
  styleUrl: './departments.component.scss'
})
export class DepartmentsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly departments = signal<Department[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly canManage = signal(false);
  readonly drawerOpen = signal(false);
  readonly saving = signal(false);
  readonly formError = signal('');
  readonly editingId = signal<string | null>(null);
  readonly updatingStatusId = signal<string | null>(null);

  searchTerm = '';
  statusFilter: number | null = null;
  name = '';

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canManage.set(user?.role === 'TenantSuperAdmin');
    this.loadDepartments();
  }

  loadDepartments() {
    this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams({ page: '1', pageSize: '50' });
    const term = this.searchTerm.trim();
    if (term) query.set('filter', term);
    if (this.statusFilter != null) query.set('status', String(this.statusFilter));

    this.api.get<ApiResult<DepartmentList>>(`/departments?${query}`).subscribe({
      next: res => {
        this.departments.set(res.data?.items ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load departments.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.loadDepartments();
  }

  clearSearch() {
    this.searchTerm = '';
    this.statusFilter = null;
    this.loadDepartments();
  }

  openCreate() {
    this.editingId.set(null);
    this.resetForm();
    this.drawerOpen.set(true);
  }

  openEdit(dept: Department) {
    this.editingId.set(dept.id);
    this.name = dept.name;
    this.formError.set('');
    this.drawerOpen.set(true);
  }

  closeDrawer() {
    if (this.saving()) return;
    this.drawerOpen.set(false);
    this.formError.set('');
  }

  resetForm() {
    this.name = '';
    this.formError.set('');
  }

  submit() {
    if (!this.name.trim()) {
      this.formError.set('Department name is required.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');

    const body = { name: this.name.trim() };
    const editId = this.editingId();
    const request = editId
      ? this.api.put<ApiResult<Department>>(`/departments/${editId}`, body)
      : this.api.post<ApiResult<Department>>('/departments', body);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.resetForm();
        this.loadDepartments();
      },
      error: err => {
        this.formError.set(err.error?.message ?? 'Unable to save department.');
        this.saving.set(false);
      }
    });
  }

  toggleStatus(dept: Department) {
    if (!this.canManage()) return;

    const newStatus = dept.statusCode === 1 ? 2 : 1;
    this.updatingStatusId.set(dept.id);
    this.error.set('');

    this.api.patch<ApiResult<boolean>>(`/departments/${dept.id}/status?status=${newStatus}`, {}).subscribe({
      next: () => {
        this.updatingStatusId.set(null);
        this.loadDepartments();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to update status.');
        this.updatingStatusId.set(null);
      }
    });
  }
}
