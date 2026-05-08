namespace algo.Application.Configuration;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int ExpirationMinutes { get; set; } = 10;

    public int CodeLength { get; set; } = 6;

    public string Pepper { get; set; } = string.Empty;
}
