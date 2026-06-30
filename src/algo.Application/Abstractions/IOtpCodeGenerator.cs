namespace algo.Application.Abstractions;

public interface IOtpCodeGenerator
{
    string GenerateNumericCode(int length);

    string HashCode(string plainCode);
}
