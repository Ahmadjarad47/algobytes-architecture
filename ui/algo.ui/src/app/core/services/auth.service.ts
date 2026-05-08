import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';

import { AuthSession } from '../models/auth-session.model';

const AUTH_SESSION_KEY = 'algo.ui.auth.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly sessionState = signal<AuthSession | null>(this.readSession());

  readonly session = this.sessionState.asReadonly();
  readonly isAuthenticated = () => Boolean(this.sessionState()?.accessToken);

  setSession(session: AuthSession): void {
    this.sessionState.set(session);
    this.writeSession(session);
  }

  clearSession(): void {
    this.sessionState.set(null);
    this.removeSession();
  }

  getAccessToken(): string | null {
    return this.sessionState()?.accessToken ?? null;
  }

  private readSession(): AuthSession | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    const rawSession = localStorage.getItem(AUTH_SESSION_KEY);
    if (!rawSession) {
      return null;
    }

    try {
      return JSON.parse(rawSession) as AuthSession;
    } catch {
      localStorage.removeItem(AUTH_SESSION_KEY);
      return null;
    }
  }

  private writeSession(session: AuthSession): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(AUTH_SESSION_KEY, JSON.stringify(session));
    }
  }

  private removeSession(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(AUTH_SESSION_KEY);
    }
  }
}
