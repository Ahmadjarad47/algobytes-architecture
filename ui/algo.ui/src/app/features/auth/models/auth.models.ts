export interface LoginCommand {
  readonly email: string;
  readonly password: string;
}

export interface RegisterCommand {
  readonly email: string;
  readonly password: string;
  readonly confirmPassword: string;
  readonly displayName: string;
}

export interface AuthResponseDto {
  readonly user: {
    readonly userId: string;
    readonly email: string;
    readonly displayName: string;
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

export interface OtpVerificationDto {
  readonly email: string;
  readonly expiresAtUtc: string;
  readonly message: string;
}
