namespace algo.Application.Abstractions;

public interface IOperationalActivityNotifier
{
    Task NotifyAsync(OperationalActivityEvent activity, CancellationToken cancellationToken = default);
}
