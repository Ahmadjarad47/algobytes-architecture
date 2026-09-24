namespace algo.Application.Abstractions.Identity;

public interface IOtpCodeVerifier
{
    bool VerifyCode(string plainCode, string codeHash);
}

