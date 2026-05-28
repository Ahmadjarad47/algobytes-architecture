export interface LoginCommand {
  readonly email: string;
  readonly password: string;
  readonly totpCode?: string;
}

export interface RegisterCommand {
  readonly email: string;
  readonly password: string;
  readonly confirmPassword: string;
  readonly displayName: string;
  readonly customFields?: Record<string, unknown>;
}

export interface AuthResponseDto {
  readonly user: {
    readonly userId: string;
    readonly email: string;
    readonly displayName: string;
    readonly roles: readonly string[];
    readonly permissions: readonly string[];
  };
  readonly tokens: {
    readonly accessToken: string;
    readonly accessTokenExpiresAt: string;
    readonly refresh: {
      readonly token: string;
      readonly expiresAtUtc: string;
    };
  };
}

export interface LoginResponseDto {
  readonly user: AuthResponseDto['user'] | null;
  readonly tokens: AuthResponseDto['tokens'] | null;
  readonly totpChallenge: {
    readonly requiresTwoFactor: boolean;
    readonly setupRequired: boolean;
    readonly setupKey: string | null;
    readonly setupUri: string | null;
    readonly message: string;
  } | null;
}

export interface OtpVerificationDto {
  readonly email: string;
  readonly expiresAtUtc: string;
  readonly message: string;
}
