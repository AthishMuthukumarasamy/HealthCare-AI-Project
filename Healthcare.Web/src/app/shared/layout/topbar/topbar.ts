import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss'
})
export class Topbar {
  protected readonly auth = inject(Auth);

  private readonly router = inject(Router);

  protected onLogout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}