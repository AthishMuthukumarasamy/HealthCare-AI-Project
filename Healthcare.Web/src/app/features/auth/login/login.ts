
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { Auth } from '../../../core/services/auth';
import { AuthApi } from '../../../core/services/auth-api';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly authApi = inject(AuthApi);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  protected email = '';
  protected password = '';
  protected selectedRole = 'Admin';

  protected readonly showPassword = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal('');

  protected togglePassword(): void {
    this.showPassword.update(value => !value);
  }

  protected onLogin(): void {
    this.errorMessage.set('');

    if (!this.email.trim() || !this.password) {
      this.errorMessage.set('Please enter your email and password.');
      return;
    }

    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    this.authApi.login({
      email: this.email.trim(),
      password: this.password
    }).subscribe({
      next: response => {
        const hasSelectedRole = response.roles.some(
          role => role.toLowerCase() === this.selectedRole.toLowerCase()
        );

        if (!hasSelectedRole) {
          this.errorMessage.set(
            `This account does not have the ${this.selectedRole} role.`
          );
          this.isLoading.set(false);
          return;
        }

        this.auth.saveLogin(response);
        this.isLoading.set(false);

        void this.router.navigate(['/dashboard']);
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);

        if (error.status === 0) {
          this.errorMessage.set(
            'Unable to connect to the server. Please try again later.'
          );
        } else if (error.status === 429) {
          this.errorMessage.set(
            'Too many login attempts. Please try again later.'
          );
        } else if (error.status === 400 || error.status === 401) {
          this.errorMessage.set(
            'Invalid email or password. Please check your credentials.'
          );
        } else {
          this.errorMessage.set(
            'Something went wrong. Please try again.'
          );
        }
      }
    });
  }
}
