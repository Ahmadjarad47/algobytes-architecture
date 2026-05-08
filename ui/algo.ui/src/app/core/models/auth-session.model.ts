export interface AuthSession {
  readonly accessToken: string;
  readonly refreshToken?: string;
  readonly accessTokenExpiresAt?: string;
  readonly user?: {
    readonly userId: string;
    readonly email: string;
    readonly displayName: string;
  };
}
