namespace algo.Application.Abstractions;

public interface IOtpService
{
    string GenerateNumericCode(int length);

    string HashCode(string plainCode);

    bool VerifyCode(string plainCode, string codeHash);
}
