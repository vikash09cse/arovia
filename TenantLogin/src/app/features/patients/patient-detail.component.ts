import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
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

interface PatientDocumentItem {
  id: string;
  patientId: string;
  displayName: string;
  storedFileName: string;
  fileType: string;
  createdAt: string;
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
  readonly message = signal('');
  readonly patient = signal<PatientDetail | null>(null);
  readonly summary = signal<VisitSummary | null>(null);
  readonly visits = signal<VisitListItem[]>([]);
  readonly documents = signal<PatientDocumentItem[]>([]);
  readonly documentsLoading = signal(false);
  readonly showUploadPanel = signal(false);
  readonly uploading = signal(false);
  readonly busyDocId = signal<string | null>(null);
  readonly canEdit = signal(false);

  selectedFile: File | null = null;

  ngOnInit() {
    const user = this.auth.currentUser();
    this.canEdit.set(user?.role === 'TenantSuperAdmin' || user?.role === 'Staff');
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.load(id);
  }

  load(id: string) {
    this.loading.set(true);
    this.error.set('');
    this.api.get<ApiResult<PatientDetail>>(`/patients/${id}`).subscribe({
      next: res => {
        this.patient.set(res.data ?? null);
        this.loadSummary(id);
        this.loadVisits(id);
        this.loadDocuments(id);
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

  loadDocuments(patientId: string) {
    this.documentsLoading.set(true);
    this.api.get<ApiResult<PatientDocumentItem[]>>(`/patients/${patientId}/documents`).subscribe({
      next: res => {
        this.documents.set(res.data ?? []);
        this.documentsLoading.set(false);
      },
      error: () => {
        this.documents.set([]);
        this.documentsLoading.set(false);
      }
    });
  }

  toggleUploadPanel() {
    this.showUploadPanel.update(v => !v);
    this.message.set('');
    this.error.set('');
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.message.set('');
    this.error.set('');
  }

  uploadDocument() {
    const patient = this.patient();
    if (!this.canEdit() || !patient || !this.selectedFile) return;

    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.uploading.set(true);
    this.error.set('');
    this.message.set('');
    this.api.postFormData<ApiResult<PatientDocumentItem>>(
      `/patients/${patient.id}/documents`,
      formData
    ).subscribe({
      next: res => {
        this.uploading.set(false);
        this.message.set(res.message || 'Document uploaded.');
        this.selectedFile = null;
        const input = document.getElementById('patient-doc-input') as HTMLInputElement | null;
        if (input) input.value = '';
        this.showUploadPanel.set(false);
        this.loadDocuments(patient.id);
      },
      error: err => {
        this.uploading.set(false);
        this.error.set(err.error?.message ?? 'Unable to upload document.');
      }
    });
  }

  canPrint(item: PatientDocumentItem): boolean {
    const type = (item.fileType || '').toUpperCase();
    return ['PDF', 'PNG', 'JPG', 'JPEG', 'WEBP', 'GIF'].includes(type);
  }

  canView(item: PatientDocumentItem): boolean {
    return this.canPrint(item);
  }

  private async fetchDocumentBlob(patientId: string, item: PatientDocumentItem): Promise<Blob> {
    const response = await firstValueFrom(
      this.api.getBlob(`/patients/${patientId}/documents/${item.id}/download`)
    );
    const blob = response.body;
    if (!blob) throw new Error('Unable to load document.');

    if (blob.type.includes('application/json')) {
      const text = await blob.text();
      try {
        const parsed = JSON.parse(text) as { message?: string };
        throw new Error(parsed.message || 'Unable to load document.');
      } catch (e) {
        if (e instanceof SyntaxError) throw new Error('Unable to load document.');
        throw e;
      }
    }

    return blob;
  }

  async viewDocument(item: PatientDocumentItem) {
    if (!this.canView(item)) return;
    const patient = this.patient();
    if (!patient) return;

    // Open immediately on the click gesture so the browser does not block the tab.
    const win = window.open('about:blank', '_blank');
    if (!win) {
      this.error.set('Pop-up blocked. Allow pop-ups to view documents.');
      return;
    }
    win.document.write(
      '<!DOCTYPE html><html><head><title>Loading…</title></head>' +
      '<body style="font-family:Segoe UI,Arial,sans-serif;padding:24px;color:#334155">Loading document…</body></html>'
    );
    win.document.close();

    this.busyDocId.set(item.id);
    this.error.set('');
    try {
      const blob = await this.fetchDocumentBlob(patient.id, item);
      const url = URL.createObjectURL(blob);
      win.location.href = url;
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (err) {
      try { win.close(); } catch { /* ignore */ }
      this.error.set(err instanceof Error ? err.message : 'Unable to view document.');
    } finally {
      this.busyDocId.set(null);
    }
  }

  async downloadDocument(item: PatientDocumentItem) {
    const patient = this.patient();
    if (!patient) return;

    this.busyDocId.set(item.id);
    this.error.set('');
    try {
      const blob = await this.fetchDocumentBlob(patient.id, item);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = item.displayName || 'document';
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Unable to download document.');
    } finally {
      this.busyDocId.set(null);
    }
  }

  async printDocument(item: PatientDocumentItem) {
    if (!this.canPrint(item)) return;
    const patient = this.patient();
    if (!patient) return;

    this.busyDocId.set(item.id);
    this.error.set('');
    try {
      const response = await firstValueFrom(
        this.api.getBlob(`/patients/${patient.id}/documents/${item.id}/download`)
      );
      const blob = response.body;
      if (!blob) throw new Error('Unable to load document for print.');

      if (blob.type.includes('application/json')) {
        const text = await blob.text();
        try {
          const parsed = JSON.parse(text) as { message?: string };
          throw new Error(parsed.message || 'Unable to print document.');
        } catch (e) {
          if (e instanceof SyntaxError) throw new Error('Unable to print document.');
          throw e;
        }
      }

      const url = URL.createObjectURL(blob);
      const iframe = document.createElement('iframe');
      iframe.setAttribute('aria-hidden', 'true');
      iframe.setAttribute('title', 'Print document');
      Object.assign(iframe.style, {
        position: 'fixed',
        right: '0',
        bottom: '0',
        width: '0',
        height: '0',
        border: '0',
        opacity: '0',
        pointerEvents: 'none'
      });
      document.body.appendChild(iframe);

      const win = iframe.contentWindow;
      if (!win) {
        iframe.remove();
        URL.revokeObjectURL(url);
        throw new Error('Unable to prepare print view.');
      }

      const cleanup = () => {
        try { iframe.remove(); } catch { /* ignore */ }
        URL.revokeObjectURL(url);
      };

      iframe.onload = () => {
        setTimeout(() => {
          win.focus();
          win.print();
          win.addEventListener('afterprint', cleanup, { once: true });
          setTimeout(cleanup, 60_000);
        }, 250);
      };

      iframe.src = url;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Unable to print document.');
    } finally {
      this.busyDocId.set(null);
    }
  }

  removeDocument(item: PatientDocumentItem) {
    const patient = this.patient();
    if (!this.canEdit() || !patient) return;
    if (!confirm(`Delete "${item.displayName}"?`)) return;

    this.busyDocId.set(item.id);
    this.error.set('');
    this.message.set('');
    this.api.delete<ApiResult<boolean>>(`/patients/${patient.id}/documents/${item.id}`).subscribe({
      next: res => {
        this.busyDocId.set(null);
        this.message.set(res.message || 'Document deleted.');
        this.loadDocuments(patient.id);
      },
      error: err => {
        this.busyDocId.set(null);
        this.error.set(err.error?.message ?? 'Unable to delete document.');
      }
    });
  }

  formatMoney(value?: number | null): string {
    if (value == null) return '—';
    return `₹${value.toLocaleString('en-IN')}`;
  }
}
