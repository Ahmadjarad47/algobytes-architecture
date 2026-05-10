namespace algo.RealTime;

public interface IOperationalActivityNotifier
{
    Task NotifyAsync(OperationalActivityEvent activity, CancellationToken cancellationToken = default);
}
