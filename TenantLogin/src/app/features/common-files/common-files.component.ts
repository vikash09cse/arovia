import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResult } from '../../core/models/api.models';

interface CommonFileItem {
  id: string;
  displayName: string;
  storedFileName: string;
  fileType: string;
  createdAt: string;
}

@Component({
  selector: 'app-common-files',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './common-files.component.html',
  styleUrl: './common-files.component.scss'
})
export class CommonFilesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly items = signal<CommonFileItem[]>([]);
  readonly loading = signal(true);
  readonly uploading = signal(false);
  readonly busyId = signal<string | null>(null);
  readonly error = signal('');
  readonly message = signal('');

  readonly isAdmin = computed(() => this.auth.currentUser()?.role === 'TenantSuperAdmin');

  selectedFile: File | null = null;

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.api.get<ApiResult<CommonFileItem[]>>('/common-files').subscribe({
      next: res => {
        this.items.set(res.data ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load common files.');
        this.loading.set(false);
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.message.set('');
    this.error.set('');
  }

  upload() {
    if (!this.isAdmin() || !this.selectedFile) return;

    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.uploading.set(true);
    this.error.set('');
    this.message.set('');
    this.api.postFormData<ApiResult<CommonFileItem>>('/common-files', formData).subscribe({
      next: res => {
        this.uploading.set(false);
        this.message.set(res.message || 'File uploaded.');
        this.selectedFile = null;
        const input = document.getElementById('common-file-input') as HTMLInputElement | null;
        if (input) input.value = '';
        this.load();
      },
      error: err => {
        this.uploading.set(false);
        this.error.set(err.error?.message ?? 'Unable to upload file.');
      }
    });
  }

  canPrint(item: CommonFileItem): boolean {
    const type = (item.fileType || '').toUpperCase();
    return ['PDF', 'PNG', 'JPG', 'JPEG', 'WEBP', 'GIF'].includes(type);
  }

  async download(item: CommonFileItem) {
    this.busyId.set(item.id);
    this.error.set('');
    try {
      const response = await firstValueFrom(this.api.getBlob(`/common-files/${item.id}/download`));
      const blob = response.body;
      if (!blob) throw new Error('Download failed.');

      if (blob.type.includes('application/json')) {
        const text = await blob.text();
        try {
          const parsed = JSON.parse(text) as { message?: string };
          throw new Error(parsed.message || 'Unable to download file.');
        } catch (e) {
          if (e instanceof SyntaxError) throw new Error('Unable to download file.');
          throw e;
        }
      }

      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = item.displayName || 'file';
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Unable to download file.');
    } finally {
      this.busyId.set(null);
    }
  }

  async print(item: CommonFileItem) {
    if (!this.canPrint(item)) return;

    this.busyId.set(item.id);
    this.error.set('');
    try {
      const response = await firstValueFrom(this.api.getBlob(`/common-files/${item.id}/download`));
      const blob = response.body;
      if (!blob) throw new Error('Unable to load file for print.');

      if (blob.type.includes('application/json')) {
        const text = await blob.text();
        try {
          const parsed = JSON.parse(text) as { message?: string };
          throw new Error(parsed.message || 'Unable to print file.');
        } catch (e) {
          if (e instanceof SyntaxError) throw new Error('Unable to print file.');
          throw e;
        }
      }

      const url = URL.createObjectURL(blob);
      const iframe = document.createElement('iframe');
      iframe.setAttribute('aria-hidden', 'true');
      iframe.setAttribute('title', 'Print file');
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
      this.error.set(err instanceof Error ? err.message : 'Unable to print file.');
    } finally {
      this.busyId.set(null);
    }
  }

  remove(item: CommonFileItem) {
    if (!this.isAdmin()) return;
    if (!confirm(`Delete "${item.displayName}"?`)) return;

    this.busyId.set(item.id);
    this.error.set('');
    this.message.set('');
    this.api.delete<ApiResult<boolean>>(`/common-files/${item.id}`).subscribe({
      next: res => {
        this.busyId.set(null);
        this.message.set(res.message || 'File deleted.');
        this.load();
      },
      error: err => {
        this.busyId.set(null);
        this.error.set(err.error?.message ?? 'Unable to delete file.');
      }
    });
  }
}
