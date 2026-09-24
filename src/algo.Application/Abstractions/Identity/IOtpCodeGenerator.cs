namespace algo.Application.Abstractions.Identity;

public interface IOtpCodeGenerator
{
    string GenerateNumericCode(int length);

    string HashCode(string plainCode);
}

