import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  AuthResponseDto,
  LoginCommand,
  OtpVerificationDto,
  RegisterCommand
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly api = inject(ApiService);

  login(request: LoginCommand): Observable<AuthResponseDto> {
    return this.api.post<AuthResponseDto, LoginCommand>('/Auth/login', request);
  }

  register(request: RegisterCommand): Observable<OtpVerificationDto> {
    return this.api.post<OtpVerificationDto, RegisterCommand>(
      '/Auth/register',
      request
    );
  }

  logout(refreshToken: string | null): Observable<void> {
    return this.api.post<void, { refreshToken: string | null }>('/Auth/logout', {
      refreshToken
    });
  }
}
