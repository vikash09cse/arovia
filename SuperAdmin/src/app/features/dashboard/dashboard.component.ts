import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TenantService } from '../../core/auth/auth.service';
import { PlatformDashboard } from '../../core/models/api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly tenantService = inject(TenantService);
  readonly dashboard = signal<PlatformDashboard | null>(null);
  readonly loading = signal(true);

  ngOnInit() {
    this.tenantService.getDashboard().subscribe({
      next: res => {
        this.dashboard.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
