import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';

interface TenantSettings {
  id: string;
  hospitalName: string;
  subdomain: string;
  primaryContactFirstName: string;
  primaryContactLastName: string;
  primaryContactEmail: string;
  primaryContactPhone: string;
  address: string;
  timezone: string;
  website?: string | null;
  logoUrl?: string | null;
  updatedAt: string;
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly uploadingLogo = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly logoPreview = signal<string | null>(null);

  hospitalName = '';
  subdomain = '';
  primaryContactFirstName = '';
  primaryContactLastName = '';
  primaryContactEmail = '';
  primaryContactPhone = '';
  address = '';
  timezone = 'Asia/Kolkata';
  website = '';
  logoUrl: string | null = null;
  selectedLogoFile: File | null = null;

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.api.get<ApiResult<TenantSettings>>('/tenant-settings').subscribe({
      next: res => {
        const s = res.data;
        if (!s) {
          this.error.set('Unable to load tenant settings.');
          this.loading.set(false);
          return;
        }
        this.hospitalName = s.hospitalName;
        this.subdomain = s.subdomain;
        this.primaryContactFirstName = s.primaryContactFirstName;
        this.primaryContactLastName = s.primaryContactLastName;
        this.primaryContactEmail = s.primaryContactEmail;
        this.primaryContactPhone = s.primaryContactPhone;
        this.address = s.address;
        this.timezone = s.timezone || 'Asia/Kolkata';
        this.website = s.website ?? '';
        this.logoUrl = s.logoUrl ?? null;
        this.logoPreview.set(this.logoUrl);
        this.selectedLogoFile = null;
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load tenant settings.');
        this.loading.set(false);
      }
    });
  }

  onLogoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedLogoFile = file;
    this.message.set('');
    this.error.set('');

    if (!file) {
      this.logoPreview.set(this.logoUrl);
      return;
    }

    if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type)) {
      this.error.set('Logo must be a PNG, JPG, or WebP image.');
      this.selectedLogoFile = null;
      input.value = '';
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      this.error.set('Logo must be 2 MB or smaller.');
      this.selectedLogoFile = null;
      input.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = () => this.logoPreview.set(typeof reader.result === 'string' ? reader.result : null);
    reader.readAsDataURL(file);
  }

  clearSelectedLogo(input: HTMLInputElement) {
    this.selectedLogoFile = null;
    input.value = '';
    this.logoPreview.set(this.logoUrl);
  }

  async save() {
    this.error.set('');
    this.message.set('');

    if (!this.hospitalName.trim()
      || !this.primaryContactFirstName.trim()
      || !this.primaryContactLastName.trim()
      || !this.primaryContactEmail.trim()
      || !this.primaryContactPhone.trim()
      || !this.address.trim()
      || !this.timezone.trim()) {
      this.error.set('Please fill in all required fields.');
      return;
    }

    this.saving.set(true);

    try {
      if (this.selectedLogoFile) {
        this.uploadingLogo.set(true);
        const formData = new FormData();
        formData.append('file', this.selectedLogoFile);
        const logoRes = await new Promise<ApiResult<TenantSettings>>((resolve, reject) => {
          this.api.postFormData<ApiResult<TenantSettings>>('/tenant-settings/logo', formData).subscribe({
            next: resolve,
            error: reject
          });
        });
        this.logoUrl = logoRes.data?.logoUrl ?? this.logoUrl;
        this.logoPreview.set(this.logoUrl);
        this.selectedLogoFile = null;
        this.uploadingLogo.set(false);
      }

      const body = {
        hospitalName: this.hospitalName.trim(),
        primaryContactFirstName: this.primaryContactFirstName.trim(),
        primaryContactLastName: this.primaryContactLastName.trim(),
        primaryContactEmail: this.primaryContactEmail.trim(),
        primaryContactPhone: this.primaryContactPhone.trim(),
        address: this.address.trim(),
        timezone: this.timezone.trim(),
        website: this.website.trim() || null,
        logoUrl: this.logoUrl
      };

      const res = await new Promise<ApiResult<TenantSettings>>((resolve, reject) => {
        this.api.put<ApiResult<TenantSettings>>('/tenant-settings', body).subscribe({
          next: resolve,
          error: reject
        });
      });

      const s = res.data;
      if (s) {
        this.hospitalName = s.hospitalName;
        this.subdomain = s.subdomain;
        this.primaryContactFirstName = s.primaryContactFirstName;
        this.primaryContactLastName = s.primaryContactLastName;
        this.primaryContactEmail = s.primaryContactEmail;
        this.primaryContactPhone = s.primaryContactPhone;
        this.address = s.address;
        this.timezone = s.timezone;
        this.website = s.website ?? '';
        this.logoUrl = s.logoUrl ?? null;
        this.logoPreview.set(this.logoUrl);
      }

      this.message.set(res.message || 'Tenant settings saved.');
    } catch (err: unknown) {
      const message = (err as { error?: { message?: string } })?.error?.message
        ?? 'Unable to save tenant settings.';
      this.error.set(message);
    } finally {
      this.saving.set(false);
      this.uploadingLogo.set(false);
    }
  }
}
