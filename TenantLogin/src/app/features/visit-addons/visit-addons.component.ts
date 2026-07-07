import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface VisitAddon {
  id: string;
  name: string;
  code?: string | null;
  defaultAmount: number;
  status: string;
  statusCode: number;
}

interface VisitAddonList {
  items: VisitAddon[];
  totalCount: number;
}

@Component({
  selector: 'app-visit-addons',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './visit-addons.component.html',
  styleUrl: './visit-addons.component.scss'
})
export class VisitAddonsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly addons = signal<VisitAddon[]>([]);
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
  code = '';
  defaultAmount: number | null = null;

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canManage.set(user?.role === 'TenantSuperAdmin');
    this.loadAddons();
  }

  loadAddons() {
    this.loading.set(true);
    this.error.set('');

    const query = new URLSearchParams({ page: '1', pageSize: '50' });
    const term = this.searchTerm.trim();
    if (term) query.set('filter', term);
    if (this.statusFilter != null) query.set('status', String(this.statusFilter));

    this.api.get<ApiResult<VisitAddonList>>(`/visit-addons?${query}`).subscribe({
      next: res => {
        this.addons.set(res.data?.items ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load visit add-ons.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.loadAddons();
  }

  clearSearch() {
    this.searchTerm = '';
    this.statusFilter = null;
    this.loadAddons();
  }

  openCreate() {
    this.editingId.set(null);
    this.resetForm();
    this.drawerOpen.set(true);
  }

  openEdit(addon: VisitAddon) {
    this.editingId.set(addon.id);
    this.name = addon.name;
    this.code = addon.code ?? '';
    this.defaultAmount = addon.defaultAmount;
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
    this.code = '';
    this.defaultAmount = null;
    this.formError.set('');
  }

  formatMoney(value: number): string {
    return `₹${value.toLocaleString('en-IN')}`;
  }

  submit() {
    if (!this.name.trim()) {
      this.formError.set('Addon name is required.');
      return;
    }
    if (this.defaultAmount == null || this.defaultAmount < 0) {
      this.formError.set('A valid amount is required.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');

    const body = {
      name: this.name.trim(),
      code: this.code.trim() || null,
      defaultAmount: this.defaultAmount
    };

    const editId = this.editingId();
    const request = editId
      ? this.api.put<ApiResult<VisitAddon>>(`/visit-addons/${editId}`, body)
      : this.api.post<ApiResult<VisitAddon>>('/visit-addons', body);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.resetForm();
        this.loadAddons();
      },
      error: err => {
        this.formError.set(err.error?.message ?? 'Unable to save visit add-on.');
        this.saving.set(false);
      }
    });
  }

  toggleStatus(addon: VisitAddon) {
    if (!this.canManage()) return;

    const newStatus = addon.statusCode === 1 ? 2 : 1;
    this.updatingStatusId.set(addon.id);
    this.error.set('');

    this.api.patch<ApiResult<boolean>>(`/visit-addons/${addon.id}/status?status=${newStatus}`, {}).subscribe({
      next: () => {
        this.updatingStatusId.set(null);
        this.loadAddons();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to update status.');
        this.updatingStatusId.set(null);
      }
    });
  }
}
