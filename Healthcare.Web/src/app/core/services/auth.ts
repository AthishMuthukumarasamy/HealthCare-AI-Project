
import { Injectable, signal } from '@angular/core';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  message: string;
  token: string;
  expiration: string;
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class Auth {
  private readonly tokenKey = 'healthconnect_auth_token';
  private readonly userKey = 'healthconnect_auth_user';

  private readonly currentUserSignal = signal<LoginResponse | null>(
    this.loadStoredUser()
  );

  readonly currentUser = this.currentUserSignal.asReadonly();

  getToken(): string | null {
    return this.currentUserSignal()?.token ?? null;
  }

  isAuthenticated(): boolean {
    const user = this.currentUserSignal();

    if (!user) {
      return false;
    }

    if (new Date(user.expiration).getTime() <= Date.now()) {
      this.logout();
      return false;
    }

    return true;
  }

  hasRole(role: string): boolean {
    return this.currentUserSignal()?.roles.some(
      assignedRole => assignedRole.toLowerCase() === role.toLowerCase()
    ) ?? false;
  }

  saveLogin(response: LoginResponse): void {
    if (!response.token || !response.expiration) {
      throw new Error('Invalid authentication response.');
    }

    sessionStorage.setItem(this.tokenKey, response.token);
    sessionStorage.setItem(this.userKey, JSON.stringify(response));

    this.currentUserSignal.set(response);
  }

  logout(): void {
    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.userKey);

    this.currentUserSignal.set(null);
  }

  private loadStoredUser(): LoginResponse | null {
    try {
      const storedUser = sessionStorage.getItem(this.userKey);

      if (!storedUser) {
        return null;
      }

      const user = JSON.parse(storedUser) as LoginResponse;

      if (
        !user.token ||
        !user.expiration ||
        new Date(user.expiration).getTime() <= Date.now()
      ) {
        sessionStorage.removeItem(this.tokenKey);
        sessionStorage.removeItem(this.userKey);
        return null;
      }

      return user;
    } catch {
      sessionStorage.removeItem(this.tokenKey);
      sessionStorage.removeItem(this.userKey);
      return null;
    }
  }
}
