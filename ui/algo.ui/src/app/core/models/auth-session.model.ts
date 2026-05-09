export interface AuthSession {
  readonly accessToken: string;
  readonly refreshToken?: string;
  readonly accessTokenExpiresAt?: string;
  readonly user?: {
    readonly userId: string;
    readonly email: string;
    readonly displayName: string;
    readonly roles?: readonly string[];
    readonly permissions?: readonly string[];
  };
}
