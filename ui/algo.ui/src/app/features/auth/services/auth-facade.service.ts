import { inject, Injectable } from '@angular/core';
import { finalize, tap } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { AuthApiService } from '../api/auth-api.service';
import { LoginCommand, RegisterCommand } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthFacadeService {
  private readonly authApi = inject(AuthApiService);
  private readonly authService = inject(AuthService);

  login(request: LoginCommand) {
    return this.authApi.login(request).pipe(
      tap((response) =>
        this.authService.setSession({
          accessToken: response.tokens.accessToken,
          refreshToken: response.tokens.refresh.token,
          accessTokenExpiresAt: response.tokens.accessTokenExpiresAt,
          user: response.user
        })
      )
    );
  }

  register(request: RegisterCommand) {
    return this.authApi.register(request);
  }

  logout() {
    const refreshToken = this.authService.session()?.refreshToken ?? null;

    return this.authApi.logout(refreshToken).pipe(
      finalize(() => this.authService.clearSession())
    );
  }
}
