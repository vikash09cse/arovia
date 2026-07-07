import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface PatientDetail {
  id: string;
  patientCode: string;
  firstName: string;
  lastName: string;
  phone: string;
  gender: string;
  age?: number | null;
  status: string;
}

interface VisitSummary {
  totalVisits: number;
  totalCharged: number;
  totalFree: number;
  upcomingScheduledSurgeryDate?: string | null;
}

interface VisitListItem {
  id: string;
  visitCode: string;
  visitDateTime: string;
  visitType: string;
  purpose: string;
  feeStatus: string;
  feeAmount?: number | null;
  procedureChargeAmount?: number | null;
  doctorFullName: string;
  aggregatedPaymentStatus: string;
}

interface VisitList {
  items: VisitListItem[];
}

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.scss'
})
export class PatientDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly patient = signal<PatientDetail | null>(null);
  readonly summary = signal<VisitSummary | null>(null);
  readonly visits = signal<VisitListItem[]>([]);
  readonly canEdit = signal(false);

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canEdit.set(user?.role === 'TenantSuperAdmin' || user?.role === 'Staff');
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.load(id);
  }

  load(id: string) {
    this.loading.set(true);
    this.api.get<ApiResult<PatientDetail>>(`/patients/${id}`).subscribe({
      next: res => {
        this.patient.set(res.data ?? null);
        this.loadSummary(id);
        this.loadVisits(id);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load patient.');
        this.loading.set(false);
      }
    });
  }

  loadSummary(patientId: string) {
    this.api.get<ApiResult<VisitSummary>>(`/patients/${patientId}/visit-summary`).subscribe({
      next: res => this.summary.set(res.data ?? null)
    });
  }

  loadVisits(patientId: string) {
    this.api.get<ApiResult<VisitList>>(`/visits?patientId=${patientId}&page=1&pageSize=50`).subscribe({
      next: res => {
        this.visits.set(res.data?.items ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  formatMoney(value?: number | null): string {
    if (value == null) return '—';
    return `₹${value.toLocaleString('en-IN')}`;
  }
}
