namespace algo.Application.Abstractions;

public interface IOtpCodeVerifier
{
    bool VerifyCode(string plainCode, string codeHash);
}
