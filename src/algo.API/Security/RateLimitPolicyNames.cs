namespace algo.API.Security;

internal static class RateLimitPolicyNames
{
    public const string AuthLogin = "auth-login";
    public const string AuthOtp = "auth-otp";
    public const string AuthPasswordReset = "auth-password-reset";
    public const string AuthRefreshToken = "auth-refresh-token";
}
