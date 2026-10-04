import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface AdmissionListItem {
  id: string;
  admissionCode: string;
  admittedAt: string;
  dischargedAt?: string | null;
  ward: string;
  bed?: string | null;
  roomClass: string;
  status: string;
  statusCode: number;
  estimatedAmount: number;
  departmentId: string;
  departmentName: string;
  patientId: string;
  patientCode: string;
  patientFirstName: string;
  patientLastName: string;
  patientFullName: string;
  doctorName: string;
}

interface AdmissionList {
  items: AdmissionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-admissions',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './admissions.component.html',
  styleUrl: './admissions.component.scss'
})
export class AdmissionsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly items = signal<AdmissionListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly canEdit = signal(false);
  readonly Math = Math;

  statusFilter: string = '1';

  ngOnInit() {
    const role = this.auth.currentUser()?.role;
    this.canEdit.set(role === 'TenantSuperAdmin' || role === 'Staff');
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set('');
    const query = new URLSearchParams({
      page: String(this.page()),
      pageSize: String(this.pageSize)
    });
    if (this.statusFilter) query.set('status', this.statusFilter);

    this.api.get<ApiResult<AdmissionList>>(`/admissions?${query}`).subscribe({
      next: res => {
        this.items.set(res.data?.items ?? []);
        this.totalCount.set(res.data?.totalCount ?? 0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load admissions.');
        this.loading.set(false);
      }
    });
  }

  onStatusChange() {
    this.page.set(1);
    this.load();
  }

  prevPage() {
    if (this.page() <= 1) return;
    this.page.update(p => p - 1);
    this.load();
  }

  nextPage() {
    if (this.page() * this.pageSize >= this.totalCount()) return;
    this.page.update(p => p + 1);
    this.load();
  }

  statusClass(code: number): string {
    if (code === 1) return 'badge-ok';
    if (code === 2) return 'badge-muted';
    return 'badge-warn';
  }
}
