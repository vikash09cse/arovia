import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';

interface TenantUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  designation?: string | null;
  role: string;
  roleCode: number;
  status: string;
  statusCode: number;
  monthlySalary?: number | null;
}

interface UserList {
  items: TenantUser[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface UserSalaryItem {
  id: string;
  userId: string;
  monthlySalary: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  notes?: string | null;
  createdAt: string;
}

interface UserDocumentItem {
  id: string;
  userId: string;
  documentType: number;
  documentTypeLabel: string;
  displayName: string;
  storedFileName: string;
  fileType: string;
  createdAt: string;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [FormsModule, ConfirmDialogComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly users = signal<TenantUser[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly drawerOpen = signal(false);
  readonly editingUser = signal<TenantUser | null>(null);
  readonly saving = signal(false);
  readonly formError = signal('');
  readonly deletingId = signal<string | null>(null);
  readonly confirmTarget = signal<TenantUser | null>(null);
  readonly currentUserId = signal<string | null>(null);

  readonly docsUser = signal<TenantUser | null>(null);
  readonly documents = signal<UserDocumentItem[]>([]);
  readonly documentsLoading = signal(false);
  readonly docsError = signal('');
  readonly docsMessage = signal('');
  readonly uploading = signal(false);
  readonly deletingDocId = signal<string | null>(null);
  documentType = 1;
  selectedFile: File | null = null;

  readonly salaryHistory = signal<UserSalaryItem[]>([]);
  readonly salaryLoading = signal(false);
  readonly salarySaving = signal(false);
  readonly salaryError = signal('');
  reviseSalaryAmount: number | null = null;
  reviseEffectiveFrom = this.todayIso();
  reviseNotes = '';

  firstName = '';
  lastName = '';
  email = '';
  designation = '';
  roleCode = 2;
  temporaryPassword = '';
  monthlySalary: number | null = null;
  salaryEffectiveFrom = this.todayIso();

  readonly isEditing = computed(() => this.editingUser() !== null);
  readonly docsDrawerOpen = computed(() => this.docsUser() !== null);

  readonly confirmMessage = computed(() => {
    const user = this.confirmTarget();
    if (!user) return '';
    const name = `${user.firstName} ${user.lastName}`.trim();
    return `${name} (${user.email}) will be removed from the Users list. The account is soft-deleted and cannot log in.`;
  });

  readonly documentTypeOptions = [
    { value: 1, label: 'Aadhar' },
    { value: 2, label: 'Certificate' },
    { value: 3, label: 'Pan' },
    { value: 4, label: 'Other' }
  ];

  ngOnInit() {
    this.currentUserId.set(this.auth.currentUser()?.userId ?? null);
    this.loadUsers();
  }

  canManageUser(user: TenantUser): boolean {
    if (user.id === this.currentUserId()) return false;
    return user.roleCode === 2 || user.roleCode === 3; // Staff / Doctor
  }

  formatSalary(amount?: number | null): string {
    if (amount == null || Number.isNaN(Number(amount))) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2
    }).format(Number(amount));
  }

  currentSalaryAmount(): number | null {
    const history = this.salaryHistory();
    if (history.length > 0) {
      const value = Number(history[0].monthlySalary);
      return Number.isFinite(value) ? value : null;
    }
    const fromUser = this.editingUser()?.monthlySalary;
    if (fromUser == null) return null;
    const value = Number(fromUser);
    return Number.isFinite(value) ? value : null;
  }

  private normalizeSalaryList(data: unknown): UserSalaryItem[] {
    if (Array.isArray(data)) return data as UserSalaryItem[];
    if (data && typeof data === 'object') return [data as UserSalaryItem];
    return [];
  }

  private parseAmount(value: number | string | null | undefined): number | null {
    if (value === null || value === undefined || value === '') return null;
    const n = typeof value === 'number' ? value : Number(value);
    return Number.isFinite(n) ? n : null;
  }

  formatDate(iso: string): string {
    if (!iso) return '—';
    const d = new Date(iso.length <= 10 ? `${iso}T00:00:00` : iso);
    return d.toLocaleDateString(undefined, { dateStyle: 'medium' });
  }

  loadUsers() {
    this.loading.set(true);
    this.error.set('');

    this.api.get<ApiResult<UserList>>('/users?page=1&pageSize=50').subscribe({
      next: res => {
        this.users.set(res.data?.items ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load users.');
        this.loading.set(false);
      }
    });
  }

  openDrawer() {
    this.resetForm();
    this.editingUser.set(null);
    this.salaryHistory.set([]);
    this.salaryError.set('');
    this.drawerOpen.set(true);
  }

  openEdit(user: TenantUser) {
    if (!this.canManageUser(user)) return;
    this.editingUser.set(user);
    this.firstName = user.firstName;
    this.lastName = user.lastName;
    this.email = user.email;
    this.designation = user.designation ?? '';
    this.roleCode = user.roleCode;
    this.temporaryPassword = '';
    this.monthlySalary = null;
    this.salaryEffectiveFrom = this.todayIso();
    this.reviseSalaryAmount = user.monthlySalary ?? null;
    this.reviseEffectiveFrom = this.todayIso();
    this.reviseNotes = '';
    this.formError.set('');
    this.salaryError.set('');
    this.drawerOpen.set(true);
    if (user.roleCode === 2) {
      this.loadSalaryHistory(user.id);
    } else {
      this.salaryHistory.set([]);
    }
  }

  closeDrawer() {
    if (this.saving() || this.salarySaving()) return;
    this.drawerOpen.set(false);
    this.editingUser.set(null);
    this.formError.set('');
    this.salaryError.set('');
    this.salaryHistory.set([]);
  }

  resetForm() {
    this.firstName = '';
    this.lastName = '';
    this.email = '';
    this.designation = '';
    this.roleCode = 2;
    this.temporaryPassword = '';
    this.monthlySalary = null;
    this.salaryEffectiveFrom = this.todayIso();
    this.reviseSalaryAmount = null;
    this.reviseEffectiveFrom = this.todayIso();
    this.reviseNotes = '';
    this.formError.set('');
  }

  onRoleChange() {
    if (this.roleCode !== 2) {
      this.monthlySalary = null;
      this.reviseSalaryAmount = null;
      this.salaryHistory.set([]);
      this.salaryError.set('');
    } else if (this.editingUser()) {
      this.loadSalaryHistory(this.editingUser()!.id);
    }
  }

  submitUser() {
    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.formError.set('First name and last name are required.');
      return;
    }

    if (this.isEditing()) {
      this.saveEdit();
      return;
    }

    if (!this.email.trim() || !this.email.includes('@')) {
      this.formError.set('A valid email is required.');
      return;
    }

    if (this.roleCode === 2 && this.monthlySalary != null && this.monthlySalary < 0) {
      this.formError.set('Monthly salary cannot be negative.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');

    const body: Record<string, unknown> = {
      email: this.email.trim(),
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      role: this.roleCode
    };
    if (this.designation.trim()) {
      body['designation'] = this.designation.trim();
    }
    if (this.temporaryPassword.trim()) {
      body['temporaryPassword'] = this.temporaryPassword.trim();
    }

    this.api.post<ApiResult<TenantUser>>('/users', body).subscribe({
      next: res => {
        const created = res.data;
        if (created && this.roleCode === 2 && this.monthlySalary != null && this.monthlySalary >= 0) {
          this.saveInitialSalary(created.id);
          return;
        }
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.resetForm();
        this.loadUsers();
      },
      error: err => {
        this.formError.set(err.error?.message ?? 'Unable to add staff member.');
        this.saving.set(false);
      }
    });
  }

  private saveInitialSalary(userId: string) {
    this.api.post<ApiResult<UserSalaryItem>>(`/users/${userId}/salaries`, {
      monthlySalary: this.monthlySalary,
      effectiveFrom: this.salaryEffectiveFrom || this.todayIso(),
      notes: 'Initial salary'
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.resetForm();
        this.loadUsers();
      },
      error: err => {
        this.saving.set(false);
        this.drawerOpen.set(false);
        this.resetForm();
        this.loadUsers();
        this.error.set(err.error?.message ?? 'User created, but salary could not be saved.');
      }
    });
  }

  private saveEdit() {
    const user = this.editingUser();
    if (!user) return;

    const salaryAmount = this.roleCode === 2 ? this.parseAmount(this.reviseSalaryAmount) : null;
    if (this.roleCode === 2 && salaryAmount != null && salaryAmount < 0) {
      this.formError.set('Monthly salary cannot be negative.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');
    this.salaryError.set('');

    const body = {
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      role: this.roleCode,
      designation: this.designation.trim() || null
    };

    this.api.put<ApiResult<TenantUser>>(`/users/${user.id}`, body).subscribe({
      next: () => {
        if (this.roleCode === 2 && salaryAmount != null) {
          this.postSalary(user.id, salaryAmount, this.reviseEffectiveFrom || this.todayIso(), this.reviseNotes.trim() || null, {
            onDone: () => this.finishEditSuccess(),
            onError: message => {
              this.saving.set(false);
              this.salaryError.set(message);
              this.loadUsers();
            }
          });
          return;
        }
        this.finishEditSuccess();
      },
      error: err => {
        this.formError.set(err.error?.message ?? 'Unable to update user.');
        this.saving.set(false);
      }
    });
  }

  private finishEditSuccess() {
    this.saving.set(false);
    this.salarySaving.set(false);
    this.drawerOpen.set(false);
    this.editingUser.set(null);
    this.resetForm();
    this.loadUsers();
  }

  private postSalary(
    userId: string,
    monthlySalary: number,
    effectiveFrom: string,
    notes: string | null,
    handlers: { onDone: () => void; onError: (message: string) => void }
  ) {
    this.salarySaving.set(true);
    this.api.post<ApiResult<UserSalaryItem>>(`/users/${userId}/salaries`, {
      monthlySalary,
      effectiveFrom,
      notes
    }).subscribe({
      next: () => {
        this.salarySaving.set(false);
        handlers.onDone();
      },
      error: err => {
        this.salarySaving.set(false);
        handlers.onError(err.error?.message ?? 'Unable to save salary.');
      }
    });
  }

  loadSalaryHistory(userId: string) {
    this.salaryLoading.set(true);
    this.salaryError.set('');
    this.api.get<ApiResult<UserSalaryItem[]>>(`/users/${userId}/salaries`).subscribe({
      next: res => {
        const list = this.normalizeSalaryList(res.data);
        this.salaryHistory.set(list);
        if (list.length > 0) {
          const latest = list[0];
          this.reviseSalaryAmount = Number(latest.monthlySalary);
          this.reviseEffectiveFrom = this.toDateInputValue(latest.effectiveFrom) || this.todayIso();
          this.reviseNotes = latest.notes?.trim() || '';
        }
        this.salaryLoading.set(false);
      },
      error: err => {
        this.salaryHistory.set([]);
        this.salaryError.set(err.error?.message ?? 'Unable to load salary history.');
        this.salaryLoading.set(false);
      }
    });
  }

  reviseSalary() {
    const user = this.editingUser();
    if (!user || this.roleCode !== 2) return;

    const amount = this.parseAmount(this.reviseSalaryAmount);
    if (amount == null || amount < 0) {
      this.salaryError.set('Enter a valid monthly salary.');
      return;
    }
    if (!this.reviseEffectiveFrom) {
      this.salaryError.set('Effective from date is required.');
      return;
    }

    this.salaryError.set('');
    this.postSalary(user.id, amount, this.reviseEffectiveFrom, this.reviseNotes.trim() || null, {
      onDone: () => {
        this.reviseNotes = '';
        this.loadSalaryHistory(user.id);
        this.loadUsers();
      },
      onError: message => this.salaryError.set(message)
    });
  }

  openDocs(user: TenantUser) {
    if (!this.canManageUser(user)) return;
    this.docsUser.set(user);
    this.docsError.set('');
    this.docsMessage.set('');
    this.selectedFile = null;
    this.documentType = 1;
    this.loadDocuments(user.id);
  }

  closeDocs() {
    if (this.uploading()) return;
    this.docsUser.set(null);
    this.documents.set([]);
    this.docsError.set('');
    this.docsMessage.set('');
    this.selectedFile = null;
  }

  loadDocuments(userId: string) {
    this.documentsLoading.set(true);
    this.api.get<ApiResult<UserDocumentItem[]>>(`/users/${userId}/documents`).subscribe({
      next: res => {
        this.documents.set(res.data ?? []);
        this.documentsLoading.set(false);
      },
      error: err => {
        this.documents.set([]);
        this.docsError.set(err.error?.message ?? 'Unable to load documents.');
        this.documentsLoading.set(false);
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.docsMessage.set('');
    this.docsError.set('');
  }

  uploadDocument() {
    const user = this.docsUser();
    if (!user || !this.selectedFile) return;

    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('documentType', String(this.documentType));

    this.uploading.set(true);
    this.docsError.set('');
    this.docsMessage.set('');

    this.api.postFormData<ApiResult<UserDocumentItem>>(
      `/users/${user.id}/documents`,
      formData
    ).subscribe({
      next: res => {
        this.uploading.set(false);
        this.docsMessage.set(res.message || 'Document uploaded.');
        this.selectedFile = null;
        const input = document.getElementById('user-doc-input') as HTMLInputElement | null;
        if (input) input.value = '';
        this.loadDocuments(user.id);
      },
      error: err => {
        this.uploading.set(false);
        this.docsError.set(err.error?.message ?? 'Unable to upload document.');
      }
    });
  }

  async downloadDocument(item: UserDocumentItem) {
    const user = this.docsUser();
    if (!user) return;
    try {
      const response = await firstValueFrom(
        this.api.getBlob(`/users/${user.id}/documents/${item.id}/download`)
      );
      const blob = response.body;
      if (!blob) throw new Error('Unable to load document.');
      if (blob.type.includes('application/json')) {
        const text = await blob.text();
        try {
          const parsed = JSON.parse(text) as { message?: string };
          throw new Error(parsed.message || 'Unable to download document.');
        } catch (e) {
          if (e instanceof SyntaxError) throw new Error('Unable to download document.');
          throw e;
        }
      }
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = item.displayName || 'document';
      a.click();
      URL.revokeObjectURL(a.href);
    } catch (err) {
      this.docsError.set(err instanceof Error ? err.message : 'Unable to download document.');
    }
  }

  deleteDocument(item: UserDocumentItem) {
    const user = this.docsUser();
    if (!user) return;
    this.deletingDocId.set(item.id);
    this.docsError.set('');
    this.api.delete<ApiResult<boolean>>(`/users/${user.id}/documents/${item.id}`).subscribe({
      next: () => {
        this.deletingDocId.set(null);
        this.loadDocuments(user.id);
      },
      error: err => {
        this.deletingDocId.set(null);
        this.docsError.set(err.error?.message ?? 'Unable to delete document.');
      }
    });
  }

  deleteUser(user: TenantUser) {
    if (!this.canManageUser(user)) return;
    this.confirmTarget.set(user);
  }

  cancelDelete() {
    if (!this.deletingId()) {
      this.confirmTarget.set(null);
    }
  }

  confirmDelete() {
    const user = this.confirmTarget();
    if (!user) return;

    this.deletingId.set(user.id);
    this.error.set('');

    this.api.delete<ApiResult<boolean>>(`/users/${user.id}`).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.confirmTarget.set(null);
        this.loadUsers();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to delete user.');
        this.deletingId.set(null);
      }
    });
  }

  private todayIso(): string {
    return new Date().toLocaleDateString('en-CA');
  }

  /** Normalize API date (DateOnly / ISO) to yyyy-MM-dd for <input type="date">. */
  private toDateInputValue(value?: string | null): string {
    if (!value) return '';
    if (/^\d{4}-\d{2}-\d{2}/.test(value)) return value.slice(0, 10);
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return '';
    return d.toLocaleDateString('en-CA');
  }
}
