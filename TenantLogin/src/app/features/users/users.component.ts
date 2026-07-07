import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';

interface TenantUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  roleCode: number;
  status: string;
  statusCode: number;
}

interface UserList {
  items: TenantUser[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly users = signal<TenantUser[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly drawerOpen = signal(false);
  readonly saving = signal(false);
  readonly formError = signal('');

  firstName = '';
  lastName = '';
  email = '';
  temporaryPassword = '';

  ngOnInit() {
    this.loadUsers();
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
    this.drawerOpen.set(true);
  }

  closeDrawer() {
    if (this.saving()) return;
    this.drawerOpen.set(false);
    this.formError.set('');
  }

  resetForm() {
    this.firstName = '';
    this.lastName = '';
    this.email = '';
    this.temporaryPassword = '';
    this.formError.set('');
  }

  submitStaff() {
    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.formError.set('First name and last name are required.');
      return;
    }
    if (!this.email.trim() || !this.email.includes('@')) {
      this.formError.set('A valid email is required.');
      return;
    }

    this.saving.set(true);
    this.formError.set('');

    const body: Record<string, unknown> = {
      email: this.email.trim(),
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      role: 2
    };
    if (this.temporaryPassword.trim()) {
      body['temporaryPassword'] = this.temporaryPassword.trim();
    }

    this.api.post<ApiResult<TenantUser>>('/users', body).subscribe({
      next: () => {
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
}
