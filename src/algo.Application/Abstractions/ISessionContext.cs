namespace algo.Application.Abstractions;

public interface ISessionContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }

    string? Location { get; }

    string Device { get; }

    string Browser { get; }

    string OperatingSystem { get; }

    bool IsSuspicious { get; }
}
