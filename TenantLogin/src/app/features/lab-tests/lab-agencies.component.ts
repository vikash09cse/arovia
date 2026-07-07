import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface LabAgency {
  id: string;
  name: string;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  notes?: string | null;
  status: string;
  statusCode: number;
}

interface LabAgencyList {
  items: LabAgency[];
  totalCount: number;
}

@Component({
  selector: 'app-lab-agencies',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './lab-agencies.component.html',
  styleUrl: './lab-agencies.component.scss'
})
export class LabAgenciesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly agencies = signal<LabAgency[]>([]);
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
  contactPerson = '';
  phone = '';
  email = '';
  address = '';
  notes = '';

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canManage.set(user?.role === 'TenantSuperAdmin');
    this.loadAgencies();
  }

  loadAgencies() {
    this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams({ page: '1', pageSize: '50' });
    const term = this.searchTerm.trim();
    if (term) query.set('filter', term);
    if (this.statusFilter != null) query.set('status', String(this.statusFilter));

    this.api.get<ApiResult<LabAgencyList>>(`/lab-agencies?${query}`).subscribe({
      next: res => {
        this.agencies.set(res.data?.items ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load lab agencies.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.loadAgencies();
  }

  clearSearch() {
    this.searchTerm = '';
    this.statusFilter = null;
    this.loadAgencies();
  }

  openCreate() {
    this.editingId.set(null);
    this.resetForm();
    this.drawerOpen.set(true);
  }

  openEdit(agency: LabAgency) {
    this.editingId.set(agency.id);
    this.name = agency.name;
    this.contactPerson = agency.contactPerson ?? '';
    this.phone = agency.phone ?? '';
    this.email = agency.email ?? '';
    this.address = agency.address ?? '';
    this.notes = agency.notes ?? '';
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
    this.contactPerson = '';
    this.phone = '';
    this.email = '';
    this.address = '';
    this.notes = '';
    this.formError.set('');
  }

  submit() {
    if (!this.name.trim()) {
      this.formError.set('Agency name is required.');
      return;
    }
    if (this.email.trim() && !this.email.includes('@')) {
      this.formError.set('A valid email is required.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');

    const body = {
      name: this.name.trim(),
      contactPerson: this.contactPerson.trim() || null,
      phone: this.phone.trim() || null,
      email: this.email.trim() || null,
      address: this.address.trim() || null,
      notes: this.notes.trim() || null
    };

    const editId = this.editingId();
    const request = editId
      ? this.api.put<ApiResult<LabAgency>>(`/lab-agencies/${editId}`, body)
      : this.api.post<ApiResult<LabAgency>>('/lab-agencies', body);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.resetForm();
        this.loadAgencies();
      },
      error: err => {
        this.formError.set(err.error?.message ?? 'Unable to save lab agency.');
        this.saving.set(false);
      }
    });
  }

  toggleStatus(agency: LabAgency) {
    if (!this.canManage()) return;

    const newStatus = agency.statusCode === 1 ? 2 : 1;
    this.updatingStatusId.set(agency.id);
    this.error.set('');

    this.api.patch<ApiResult<boolean>>(`/lab-agencies/${agency.id}/status?status=${newStatus}`, {}).subscribe({
      next: () => {
        this.updatingStatusId.set(null);
        this.loadAgencies();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to update status.');
        this.updatingStatusId.set(null);
      }
    });
  }
}
