import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PortalUserService } from '../../core/auth/auth.service';
import { PortalUser } from '../../core/models/api.models';

@Component({
  selector: 'app-portal-users',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './portal-users.component.html',
  styleUrl: './portal-users.component.scss'
})
export class PortalUsersComponent implements OnInit {
  private readonly portalUserService = inject(PortalUserService);

  readonly users = signal<PortalUser[]>([]);

  ngOnInit() {
    this.portalUserService.getUsers().subscribe(res => this.users.set(res.data?.items ?? []));
  }

  formatRole(role: string): string {
    return role.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}
